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

namespace ResponsesATSPersonal
{
    public class ATRResponsePub : ResponseTemplateBasePub
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
        // Instance of ATR indicator
        ATSGlobalIndicatorPersonal.ATR _ATRFromATSGlobalIndicator = null;
        // Instance of SMA indicator
        ATSGlobalIndicator.SMA _SMAFromATSGlobalIndicator = null;
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
        int _shutdowntime = 151200;
        //
        Boolean _side = true;
        //
        Decimal _adj = 1;
        //
        decimal _quasiMarketOrderAdjSize = 10m;
        //
        int _entrysize = 1;
        // indicate whether using indicator from ATSGlobal
        Boolean _useATRFromATSGlobalIndicator = true;
        // average period
        int _period = 14;

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
        [Description("BarType")]
        public BarInterval BarType { get { return _barType; } set { _barType = value; } }
        [Description("NumItemPerBar")]
        public int NumItemPerBar { get { return _numTickPerBar; } set { _numTickPerBar = value; } }
        [Description("GenericParameter")]
        public String GenericParameter { get { return _genericParameter; } set { _genericParameter = (value); } }
        [Description("Total Profit")]
        public decimal TotalProfit { get { return _totalprofit; } set { _totalprofit = value; } }
        [Description("Shutdown time")]
        public int Shutdown { get { return _shutdowntime; } set { _shutdowntime = value; } }
        [Description("Entry size when signal is found")]
        public int EntrySize { get { return _entrysize; } set { _entrysize = value; } }
        [Description("UseATRFromATSGlobalIndicator")]
        public Boolean UseATRFromATSGlobalIndicator { get { return _useATRFromATSGlobalIndicator; } set { _useATRFromATSGlobalIndicator = value; } }
        [Description("Bars back when calculating sma and atr")]
        public int Period { get { return _period; } set { _period = value; } }

        /// <summary>
        /// This method will be delegated in constructor method "ATRResponsePub(bool prompt)" to "_active" tracker
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
        /// Constructor.
        /// </summary>
        /// <param name="prompt">Specifying whether we are enabling prompting of system parameters to user</param>
        public ATRResponsePub(bool prompt)
        {
            // set "_black" parameter
            _black = !prompt;
            // handle when new symbols are added to the active tracker
            _active.NewTxt += new TextIdxDelegate(_active_NewTxt);

            // set our indicator names, in case we import indicators into R
            // or excel, or we want to view them in gauntlet or kadina
            Indicators = new string[] { "Time", "SMA", "ATR[‰]", "LastClose" };
        }

        /// <summary>
        /// Default constructor. The default settting is enabling prompting of parameters
        /// </summary>
        public ATRResponsePub() : this(true) { }

        decimal ComputeSMA(Bar lastBar)
        {
            decimal SMA = decimal.MinValue;
            _SMAFromATSGlobalIndicator.UpdateValue(lastBar.Close);
            SMA = _SMAFromATSGlobalIndicator.GetSignal();
            return SMA;
        }

        /// <summary>
        /// Compute ATR indicator
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        decimal ComputeATR(Bar lastBar, decimal currentSMA)
        {
            decimal ATR = decimal.MinValue;
            // calculate the ATR using closing prices for so many bars back
            if (UseATRFromATSGlobalIndicator)
            {
                _ATRFromATSGlobalIndicator.UpdateValue(lastBar.High, lastBar.Low, lastBar.Close, currentSMA);
                ATR = _ATRFromATSGlobalIndicator.GetSignal();
            }
            else
            {
                //SMA = Calc.Avg(Calc.EndSlice(_blt[symbol].Open(), _barsback));
            }
            // this way we can debug our indicators during development
            // indicators are sent in the same order as they are named above
            return ATR;
        }

        void DoStrategy(Tick tOther, decimal ATR)
        {

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
            BarList _myBars = _blt[symbol, interval];
            // If we only get one bar, exit
            if (_myBars.Count <= 1) return;

            // make sure the lastBar is full
            Bar _lastBar = _myBars[_myBars.Count - 2];
            // Get SMA indicator
            decimal SMA = ComputeSMA(_lastBar);
            // Get ATR indicator
            decimal ATR = ComputeATR(_lastBar, SMA);

            string[] indicators = new string[]{
                _time.ToString(), 
                SMA.ToString("F5",System.Globalization.CultureInfo.InvariantCulture),
                (ATR*1000).ToString("F5", System.Globalization.CultureInfo.InvariantCulture), 
                _lastBar.Close.ToString("F5", System.Globalization.CultureInfo.InvariantCulture) 
            };

            sendindicators(indicators);

            // Retrieve tick signal
            Tick tOther = _kt[symbol];
            // Buy or sell
            // Strategy based on computed ATR indicator
            DoStrategy(tOther, ATR);
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
        /// Initialize ATR indicator
        /// </summary>
        void Initialize_ATRFromATSGlobalIndicator()
        {
            // Initializing parameters
            Hashtable ATRInputParamTmp = new Hashtable();
            ATRInputParamTmp["period"] = _period;
            _ATRFromATSGlobalIndicator = new ATSGlobalIndicatorPersonal.ATR();
            _ATRFromATSGlobalIndicator.Param = ATRInputParamTmp;

            // Call initialization method
            _ATRFromATSGlobalIndicator.Initializatin();
        }

        /// <summary>
        /// Initialize SMA indicator
        /// </summary>
        void Initialize_SMAFromATSGlobalIndicator()
        {
            // Initializing parameters
            Hashtable SMAInputParamTmp = new Hashtable();
            SMAInputParamTmp["lookbackperiod"] = _period;
            SMAInputParamTmp["minmumsignallength"] = 1;
            _SMAFromATSGlobalIndicator = new ATSGlobalIndicator.SMA();
            _SMAFromATSGlobalIndicator.Param = SMAInputParamTmp;

            // Call initialization method
            _SMAFromATSGlobalIndicator.Initializatin();
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
            // Initialize ATR indicator
            Initialize_ATRFromATSGlobalIndicator();
            // Initialize SMA indicator
            Initialize_SMAFromATSGlobalIndicator();
            // set to false to indicate the system is on
            _isShutDown = false;
            // clear total profit
            TotalProfit = 0m;
            // Clear all position
            _pt.Clear();
        }

        void SyncDateTime(Tick tick)
        {
            // keep track of time from tick
            _time = tick.time;
            if (_date != tick.date)
            {
                // this is important for consistency between runs for ATSTestBench and ATSTestBenchBatch
                Reset();
            }
            _date = tick.date;
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
        }

        /// <summary>
        /// shutdown the response system
        /// </summary>
        void shutdown()
        {
            D("shutting down everything");
            foreach (Position p in _pt)
            {
                // Flatten all positions
                if (!_wait[p.symbol] && !_pt[p.symbol].isFlat)
                {
                    Tick tOther = _kt[p.symbol];
                    _side = !p.isLong;
                    _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
                    Int64 _orderidLocal = _idtLocal.AssignId;
                    LimitOrder lOrder = new LimitOrder(p.symbol, _side, EntrySize, tOther.trade - _adj, _orderidLocal);
                    sendorder(lOrder);
                }
            }
            // Set shutdown flag
            _isShutDown = true;
        }
    }

    /// <summary>
    /// this is the same as ATRResponse, except it runs without prompting user
    /// This response will be used in "AutoRun" mode when we are processing days of tick data.
    /// </summary>
    public class ATRResponseAutoPub : ATRResponsePub
    {
        public ATRResponseAutoPub() : base(false) { }
    }
}
