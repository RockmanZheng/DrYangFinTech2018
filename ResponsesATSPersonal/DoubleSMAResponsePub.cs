﻿#define DEBUG
//#undef DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonUtilityAlbert;
using CommonUtilityAlbertA;
using TradeLink.Common;
using TradeLink.API;
using System.ComponentModel;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Data;
using BaseUtility;

namespace ResponsesATSPersonal
{
    public class DoubleSMAResponsePub : ResponseTemplateBasePub
    {
        // Determining whether we are enabling prompting of system parameters to user
        // when "_black" is "false", meaning enabled prompting
        bool _black = false;
        // Specifies the interval of one bar (candlestick)
        // e.g. 1 day, 1 hr, 30 min, etc.
        // Here the default setting is "CustomTime", which means we let user to customize the interval of bars
        BarInterval _barType = BarInterval.CustomTime;//"Time","Volume"
        /* This variable is used when "_barType" is "CustomTime".
         * Specifies the number of ticks in each bar, thus customizing the interval of bars
         */
        int _numTickPerBar = 60;

        // Instance of SMA-RSI indicator
        ATSGlobalIndicatorPersonal.DoubleSMA _DoubleSMA;


        // generic parameters
        String _genericParameter = String.Empty;
        // flag of shutdown
        // indicating whether the response system is shutdown
        bool _isShutDown = false;
        // total profit
        decimal _totalprofit = 0m;
        // Date and time of current tick
        int _time = 0;
        int _date = 0;
        // Typical shutdown time of the market is 15:00:00 every workday
        // After market is shutdown, no trade is made
        // So here we set the shutdown time limit to be 12 min late, that is 15:12:00
        int _shutdowntime = 143000;
        // specifies whether the order is a buy or sell one
        Boolean _side = true;
        // Adjustment to the order
        Decimal _adj = 1;
        // 
        decimal _quasiMarketOrderAdjSize = 10m;

        decimal _stopOrderAdjRatio = 0.1m;

        // 
        int _entrysize = 100;
        //// indicate whether using indicator from ATSGlobal
        //Boolean _useATRFromATSGlobalIndicator = true;
        //// average period
        //int _period = 14;
        // fast SMA period
        int _fastSMAPeriod=15;
        // slow SMA period
        int _slowSMAPeriod;
        // slow and fast SMA period difference
        int _slowFastDiff=20;

        // Store the last bar used in "blt_GotNewBar"s
        Bar _lastBar;
        // Store the last signal
        decimal _lastSignal = 0;
        // indicate whether we stop loss
        bool _isStop = true;


        // Indicator titles
        // These titles will be shown in the first line of the table in "indicators" panel
        string[] _indicators = new string[] { "Time", "SMA Difference", "LastClose" };

        string[] GetDisplayIndicators()
        {
            List<string> indicators = new List<string>();
            indicators.Add(_time.ToString());
            indicators.Add(_DoubleSMA.signal.ToString());
            indicators.Add(_lastBar.Close.ToString());
            return indicators.ToArray();
        }

        // Trackers
        // track if responding certain tick signal
        GenericTracker<bool> _active = new GenericTracker<bool>();
        // wait for fill
        GenericTracker<bool> _wait = new GenericTracker<bool>();
        // turn on bar tracking
        BarListTracker _blt = new BarListTracker();
        // turn on position tracking
        PositionTracker _pt = new PositionTracker();
        // Use tick tracking
        TickTracker _kt = new TickTracker();
        // Use id tracking
        IdTracker _idtLocal = new IdTracker(0);

        // parameters of this system   
        // UI of parameters
        [Description("Bar Type")]
        public BarInterval BarType { get { return _barType; } set { _barType = value; } }
        [Description("Number of Items Per Bar")]
        public int NumItemPerBar { get { return _numTickPerBar; } set { _numTickPerBar = value; } }
        [Description("GenericParameter")]
        public String GenericParameter { get { return _genericParameter; } set { _genericParameter = (value); } }
        [Description("Total Profit")]
        public decimal TotalProfit { get { return _totalprofit; } set { _totalprofit = value; } }
        [Description("Shutdown time")]
        public int Shutdown { get { return _shutdowntime; } set { _shutdowntime = value; } }
        [Description("Entry size when signal is found")]
        public int EntrySize { get { return _entrysize; } set { _entrysize = value; } }
        //[Description("UseATRFromATSGlobalIndicator")]
        //public Boolean UseATRFromATSGlobalIndicator { get { return _useATRFromATSGlobalIndicator; } set { _useATRFromATSGlobalIndicator = value; } }
        //[Description("Bars back when calculating sma and atr")]
        //public int Period { get { return _period; } set { _period = value; } }
        [Description("Bars back when calculating fast SMA")]
        public int FastSMAPeriod { get { return _fastSMAPeriod; } set { Console.WriteLine("Hey!"); _fastSMAPeriod = value; } }
        [Description("Difference between slow ema period and fast ema period")]
        public int SlowFastDiff { get { return _slowFastDiff; } set { _slowFastDiff = value; } }
        [Description("Toggle stop loss option")]
        public bool IsStop { get { return _isStop; } set { _isStop = value; } }
#if EXTERNAL_PARAM
        //从外部文件读取系统/策略参数，可向Public int/decimal/string等类型的属性传值
        string _externalParamPath = @".\..\ControlStation\ControlParam\SMAExternalParam.csv";
        [Description("SMAExternalParam")]
        public string SMAExternalParam { get { return _externalParamPath; } set { _externalParamPath = value; } }
#endif

#if DEBUG
        public string debugFileName = "debug.log";
        public FileStream debugFile;
        public StreamWriter debugWriter;
        public Random debugRnd = new Random();
        public string debugLog = "";
#endif
#if DEBUG
        public void FormatLog(string msg)
        {
            string line = "[" + DateTime.Now.ToString() + "] " + msg + Environment.NewLine;
            debugLog += line;
        }
#endif

        /// <summary>
        /// This method will be delegated in constructor method "SMARSIResponsePub(bool prompt)" to "_active" tracker
        /// Whenever new symbols are added to the "_active" tracker, this method will be called
        /// </summary>
        /// <param name="txt">The added symbol</param>
        /// <param name="idx"></param>
        void _active_NewTxt(string txt, int idx)
        {
            // go ahead and notify any other trackers about this symbol
            _wait.addindex(txt, false);
        }

        /// <summary>
        /// Initialize "_blt", the bar list tracker
        /// </summary>
        void Initialize_blt()
        {
            Int32 numOfInterval = 1;
            BarInterval[] intervaltypes = new BarInterval[numOfInterval];
            Int32[] intervalValues = new Int32[numOfInterval];
            for (Int32 i = 0; i < numOfInterval; i++)
            {
                intervaltypes[i] = BarType;
                intervalValues[i] = NumItemPerBar;
            }
            _blt = new BarListTracker(intervalValues, intervaltypes);
            // Delegate handler when we got new bar
            _blt.GotNewBar += new SymBarIntervalDelegate(blt_GotNewBar);
        }

        /// <summary>
        /// Implementation needed
        /// </summary>
        void Initialize_Indicators()
        {
            // Create SMA-RSI indicator instance
            _slowSMAPeriod = _fastSMAPeriod + _slowFastDiff;
            _DoubleSMA = new ATSGlobalIndicatorPersonal.DoubleSMA(_fastSMAPeriod, _slowSMAPeriod);
        }

        /// <summary>
        /// Reset the respose system
        /// </summary>
        public override void Reset()
        {
            // enable prompting of system parameters to user,
            // so they do not have to recompile to change things
            ParamPrompt.Popup(this, true, _black);

            // Initialize "_blt", the bar list tracker
            Initialize_blt();
            // Update generic parameters
            GenericParamUpdateHelper.updateParam(this, GenericParameter, false);
            //GenericParamUpdateHelper.updateParam(this, FastSMAPeriod, false);
            //GenericParamUpdateHelper.updateParam(this, SlowFastDiff, false);
#if EXTERNAL_PARAM
            //传递优化参数或系统参数
            GenericParamUpdateHelper.updateParam(this, GenericParameter, false);
            if (File.Exists(SMAExternalParam))
            {
                DataTable paramTableInput = null;
                StreamReader sr = new StreamReader(SMAExternalParam, Encoding.GetEncoding("GB18030"));
                paramTableInput = CsvParser.Parse(sr, true);
                sr.Close();
                foreach (DataRow dr in paramTableInput.Rows)
                {
                    if (paramTableInput.Columns.Contains("key") && paramTableInput.Columns.Contains("value"))
                    {
                        string tmpKey = dr["key"].ToString();
                        string tmpValue = dr["value"].ToString();
                        if (tmpKey == "genericparam")
                        {
                            GenericParameter = tmpValue;
                        }
                        //传递外部参数
                        GenericParamUpdateHelper.updateParam(this, GenericParameter, false);
                    }
                }
            }
#endif
            Initialize_Indicators();
            // set to false to indicate the system is on
            _isShutDown = false;
            // clear total profit
            TotalProfit = 0m;
            // Clear all position
            _pt.Clear();
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="prompt">Specifying whether we are enabling prompting of system parameters to user</param>
        public DoubleSMAResponsePub(bool prompt)
        {
            // set "_black" parameter
            _black = !prompt;
            // handle when new symbols are added to the active tracker
            _active.NewTxt += new TextIdxDelegate(_active_NewTxt);

            // set our indicator names, in case we import indicators into R
            // or excel, or we want to view them in gauntlet or kadina
            Indicators = _indicators;
        }

        /// <summary>
        /// Default constructor. The default settting is enabling prompting of parameters
        /// </summary>
        public DoubleSMAResponsePub() : this(true) { }

        /// <summary>
        /// Implementation needed
        /// </summary>
        /// <param name="lastBar"></param>
        /// <returns></returns>
        decimal ComputeSignal(Bar lastBar)
        {
            decimal signal;
            _DoubleSMA.UpdateValue(lastBar.Close);
            signal = _DoubleSMA.GetSignal();
            return signal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="tOther"></param>
        /// <param name="message"></param>
        void Buy(string symbol, decimal signal, string message = null)
        {
            Tick tOther = _kt[symbol];

            if (message != null)
            {
                D(message);
            }
            // Specify we are buying
            _side = true;
            _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
            Int64 _orderidLocal = _idtLocal.AssignId;
            int size = (int)Math.Round(EntrySize * signal);
            Order order;
            order = new BuyMarket(symbol, EntrySize, _orderidLocal);
            sendorder(order);

            // If stop loss option is specified
            if (_isStop)
            {
                //BuyStop bsOrder = new BuyStop(symbol,size,
                //order = new BuyStop(symbol, EntrySize, tOther.trade + _adj, _orderidLocal);
                //order = new SellStop(symbol, EntrySize, tOther.trade + _adj, _orderidLocal);
                //sendorder(order);

            }
            else
            {
                //order = new BuyLimit(symbol, EntrySize, tOther.trade - _adj, _orderidLocal);
                //BuyLimit blOrder = new BuyLimit(symbol, (int)Math.Round(EntrySize * signal), tOther.trade - _adj, _orderidLocal);
                //sendorder(blOrder);

            }

            // wait for fill
            _wait[symbol] = true;
        }

        void SellStop(string symbol, decimal signal, string message = null)
        {
            Tick tOther = _kt[symbol];

            if (message != null)
            {
                D(message);
            }
            // Specify we are buying

            Int64 _orderidLocal = _idtLocal.AssignId;
            int size = (int)Math.Round(EntrySize * signal);
            Order order;

            order = new SellStop(symbol, EntrySize, tOther.trade * (1 - _stopOrderAdjRatio), _orderidLocal);
            sendorder(order);

            // wait for fill
            _wait[symbol] = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="tOther"></param>
        /// <param name="message"></param>
        void Sell(string symbol, decimal signal, string message = null)
        {
            Tick tOther = _kt[symbol];

            if (message != null)
            {
                D(message);
            }
            _side = false;
            _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
            Int64 _orderidLocal = _idtLocal.AssignId;
            int size = (int)Math.Round(EntrySize * signal);
            //LimitOrder lOrder = new LimitOrder(symbol, _side, EntrySize, tOther.trade - _adj, _orderidLocal);
            // If stop loss option is specified
            Order order;
            order = new SellMarket(symbol, EntrySize, _orderidLocal);
            sendorder(order);

            if (_isStop)
            {
                //order = new BuyStop(symbol, EntrySize, tOther.trade + _adj, _orderidLocal);
                //sendorder(order);

            }
            else
            {
                //SellLimit slOrder = new SellLimit(symbol, (int)Math.Round(EntrySize * signal), tOther.trade - _adj, _orderidLocal);
                //sendorder(slOrder);
                //order = new SellLimit(symbol, EntrySize, tOther.trade - _adj, _orderidLocal);
            }

            // wait for fill
            _wait[symbol] = true;
        }

        /// <summary>
        /// Implementation needed
        /// </summary>
        /// <param name="tOther"></param>
        /// <param name="signal"></param>
        void DoStrategy(string symbol, decimal signal)
        {
            // ensure we aren't waiting for previous order to fill
            // And signal has changed direction
            if (!_wait[symbol] && _lastSignal * signal < 0)
            {
                if (signal > 0)
                {
                    Buy(symbol, signal, "Buy signal occured");
                }
                else
                {
                    Sell(symbol, signal, "Sell signal occured");
                }
            }
            if (Math.Abs(signal) > 1e-5m)
            {
                _lastSignal = signal;
            }
        }

        /// <summary>
        /// This function will be delegated in "Reset" method
        /// and will be called whenever the system got a new bar
        /// The stratege should be contained in this method
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="interval"></param>
        void blt_GotNewBar(string symbol, int interval)
        {
            BarList myBars = _blt[symbol, interval];
            // If we only get one bar, exit
            if (myBars.Count <= 1) return;
            // make sure the lastBar is full
            _lastBar = myBars[myBars.Count - 2];
            decimal signal = ComputeSignal(_lastBar);
            string[] indicators = GetDisplayIndicators();
            sendindicators(indicators);
            // Buy or sell
            // Strategy based on computed ATR indicator
            DoStrategy(symbol, signal);
        }

        void SyncDateTime(Tick tick)
        {
            // keep track of time from tick
            _time = tick.time;
            if (_date > tick.date)
            {
                // this is important for consistency between runs for ATSTestBench and ATSTestBenchBatch
                Reset();
            }
            _date = tick.date;
        }

        /// <summary>
        /// shutdown the response system
        /// </summary>
        void shutdown()
        {
            //D("shutting down everything");
            foreach (Position p in _pt)
            {
                // Flatten all positions
                if (!_wait[p.symbol] && !_pt[p.symbol].isFlat)
                {
                    Tick tOther = _kt[p.symbol];
                    _side = !p.isLong;
                    _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
                    Int64 _orderidLocal = _idtLocal.AssignId;
                    LimitOrder lOrder = new LimitOrder(p.symbol, _side, p.FlatSize, tOther.trade - _adj, _orderidLocal);
                    sendorder(lOrder);
                }
            }
#if DEBUG
            //FormatLog(_pt[0].ToString());
            FormatLog("Shut Down System");
            debugFile = new FileStream(debugFileName, FileMode.Append);
            debugWriter = new StreamWriter(debugFile);
            debugWriter.Write(debugLog);
            debugWriter.Close();
            debugFile.Close();
#endif
            // Set shutdown flag
            _isShutDown = true;
        }

        /// <summary>
        /// Called whenever we got a tick signal
        /// </summary>
        /// <param name="k"></param>
        public override void GotTick(Tick k)
        {
            SyncDateTime(k);

            // ensure response is active
            if (!isValid) return;

            // ensure we are tracking active status for this symbol
            int idx = _active.addindex(k.symbol, true);
            // if we're not active, quit
            if (!_active[idx]) return;

            // check for shutdown time
            if (k.time > Shutdown)
            {
                // if so, shutdown the system
                if (!_isShutDown)
                {
                    shutdown();
                }
                // and quit               
                return;
            }
            else
            {
                // make sure the flag is on
                _isShutDown = false;
            }
            // apply bar tracking to all ticks that enter
            _kt.newTick(k);
            _blt.newTick(k);

            // ignore anything that is not a trade
            if (!k.isTrade) return;
        }

        /// <summary>
        /// Called whenever we got a trade signal
        /// </summary>
        /// <param name="f"></param>
        public override void GotFill(Trade fill)
        {
            // make sure every fill is tracked against a position
            int sizebefore = _pt[fill.symbol].Size;
            _pt.Adjust(fill);
            bool isclosing = (sizebefore) * fill.xsize < 0;
            if (isclosing)
            {
                decimal pl = Calc.Sum(Calc.AbsoluteReturn(_pt));
                TotalProfit = pl;
            }
            // get index for this symbol
            int idx = _wait.getindex(fill.symbol);
            // ignore unknown symbols
            if (idx < 0) return;
            // stop waiting
            _wait[fill.symbol] = false;
            //Console.WriteLine(_pt[0]);
#if DEBUG
            FormatLog(_pt[0].ToString());
            //debugWriter.WriteLine(line);
#endif
        }

        //~DoubleSMAResponsePub()
        //{
        //    if (!_isShutDown)
        //    {
        //        shutdown();
        //        _isShutDown = true;
        //    }
        //}
    }

    /// <summary>
    /// this is the same as base class response, except it runs without prompting user parameters setting interface
    /// This response will be used in "AutoRun" mode when we are processing multiple tick data files
    /// </summary>
    public class DoubleSMAResponseAutoPub : DoubleSMAResponsePub
    {
        public DoubleSMAResponseAutoPub() : base(false) { }
    }
}
