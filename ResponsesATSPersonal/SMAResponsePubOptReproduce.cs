using System;
using System.Collections;
using System.Collections.Generic;
using TradeLink.Common;
using TradeLink.API;
using System.ComponentModel;
using System.Reflection;
using System.IO;
using System.Data;
using System.Text;
using CommonUtilityAlbert;
using CommonUtilityAlbertA;
using ATSGlobalIndicator;
using ATSGlobalIndicatorPersonal;
using BaseUtility;

namespace ResponsesATSPersonal
{
    // make sure your response is based on ResponseTemplateBasePub
    public class SMAResponsePubOptReproduce : ResponseTemplateBasePub
    {
        // parameters of this system       
        [Description("BarType")]
        public BarInterval BarType { get { return _barType; } set { _barType = value; } }
        [Description("NumItemPerBar")]
        public int NumItemPerBar { get { return _numTickPerBar; } set { _numTickPerBar = value; } }
        [Description("Bars back when calculating sma")]
        public int BarsBack { get { return _barsback; } set { _barsback = value; } }
        [Description("Shutdown time")]
        public int Shutdown { get { return _shutdowntime; } set { _shutdowntime = value; } }
        [Description("GenericParameter")]
        public String GenericParameter { get { return _genericParameter; } set { _genericParameter = (value); } }
        [Description("UseSMAFromATSGlobalIndicator")]
        public Boolean UseSMAFromATSGlobalIndicator { get { return _useSMAFromATSGlobalIndicator; } set { _useSMAFromATSGlobalIndicator = value; } }
        [Description("Entry size when signal is found")]
        public int EntrySize { get { return _entrysize; } set { _entrysize = value; } }
        [Description("Total Profit")]
        public decimal TotalProfit { get { return _totalprofit; } set { _totalprofit = value; } }

        public SMAResponsePubOptReproduce() : this(true) { }
        public SMAResponsePubOptReproduce(bool prompt)
        {
            _black = !prompt;
            // handle when new symbols are added to the active tracker
            _active.NewTxt += new TextIdxDelegate(_active_NewTxt);

            // set our indicator names, in case we import indicators into R
            // or excel, or we want to view them in gauntlet or kadina
            Indicators = new string[] { "Time", "SMA", "LastClose" };
        }
        public override void Reset()
        {
            // enable prompting of system parameters to user,
            // so they do not have to recompile to change things
            ParamPrompt.Popup(this, true, _black);

            Int32 numOfInterval = 1;
            BarInterval[] intervaltypes = new BarInterval[numOfInterval];
            Int32[] intervalValues = new Int32[numOfInterval];
            for (Int32 i = 0; i < numOfInterval; i++)
            {
                intervaltypes[i] = BarType;
                intervalValues[i] = NumItemPerBar;
            }

            _blt = new BarListTracker(intervalValues, intervaltypes);

            _blt.GotNewBar += new SymBarIntervalDelegate(blt_GotNewBar);

            #region <在策略中实现从外部文件读取参数>
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
            #endregion

            Hashtable SMAInputParamTmp = new Hashtable();
            SMAInputParamTmp["lookbackperiod"] = _barsback;
            SMAInputParamTmp["minmumsignallength"] = 1;
            _SMAFromATSGlobalIndicator = new ATSGlobalIndicatorPersonal.SMA();
            _SMAFromATSGlobalIndicator.Param = SMAInputParamTmp;
            _SMAFromATSGlobalIndicator.Initializatin();
            _isShutDown = false;
            TotalProfit = 0m;
            _pt.Clear();
        }
        void _active_NewTxt(string txt, int idx)
        {
            // go ahead and notify any other trackers about this symbol
            _wait.addindex(txt, false);
        }
        void blt_GotNewBar(string symbol, int interval)
        {

            int idx = _active.getindex(symbol);
            Tick tOther = _kt[symbol];

            // calculate the SMA using closign prices for so many bars back
            decimal SMA = decimal.MinValue;
            BarList _myBars = _blt[symbol, interval];
            // make sure the lastBar is full
            Bar _lastBar = _myBars[_myBars.Count - 2];
            if (UseSMAFromATSGlobalIndicator)
            {
                _SMAFromATSGlobalIndicator.UpdateValue(_lastBar.Close);
                SMA = _SMAFromATSGlobalIndicator.GetSignal();
            }
            else
            {
                SMA = Calc.Avg(Calc.EndSlice(_blt[symbol].Open(), _barsback));
            }
            // wait until we have an SMA
            if (SMA == 0)
                return;


            //ensure we aren't waiting for previous order to fill
            if (!_wait[symbol])
            {

                // if we're flat and not waiting
                if (_pt[symbol].isFlat)
                {
                    // if our current price is above SMA, buy
                    if (_blt[symbol].RecentBar.Close > SMA)
                    {
                        D("crosses above MA, buy");
                        //sendorder(new BuyMarket(symbol, EntrySize));
                        _side = true;
                        _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
                        Int64 _orderidLocal = _idtLocal.AssignId;
                        LimitOrder lOrder = new LimitOrder(symbol, _side, EntrySize, tOther.trade - _adj, _orderidLocal);
                        sendorder(lOrder);
                        // wait for fill
                        _wait[symbol] = true;
                    }
                    // otherwise if it's less than SMA, sell
                    if (_blt[symbol].RecentBar.Close < SMA)
                    {
                        D("crosses below MA, sell");
                        //sendorder(new SellMarket(symbol, EntrySize));
                        _side = false;
                        _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
                        Int64 _orderidLocal = _idtLocal.AssignId;
                        LimitOrder lOrder = new LimitOrder(symbol, _side, EntrySize, tOther.trade - _adj, _orderidLocal);
                        sendorder(lOrder);
                        // wait for fill
                        _wait[symbol] = true;
                    }
                }
                else if ((_pt[symbol].isLong && (_blt[symbol].RecentBar.Close < SMA))
                    || (_pt[symbol].isShort && (_blt[symbol].RecentBar.Close > SMA)))
                {
                    D("counter trend, exit.");
                    //sendorder(new MarketOrderFlat(pt[symbol]));
                    _side = !_pt[symbol].isLong;
                    _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
                    Int64 _orderidLocal = _idtLocal.AssignId;
                    LimitOrder lOrder = new LimitOrder(symbol, _side, EntrySize, tOther.trade - _adj, _orderidLocal);
                    sendorder(lOrder);
                    // wait for fill
                    _wait[symbol] = true;
                }
            }

            // this way we can debug our indicators during development
            // indicators are sent in the same order as they are named above
            sendindicators(new string[] { _time.ToString(), SMA.ToString("F5", System.Globalization.CultureInfo.InvariantCulture), _lastBar.Close.ToString("F5", System.Globalization.CultureInfo.InvariantCulture) });
        }
        // got tick is called whenever this strategy receives a tick
        public override void GotTick(Tick tick)
        {
            // keep track of time from tick
            _time = tick.time;
            if (_date != tick.date)
            {
                // this is important for consistency between runs for ATSTestBench and ATSTestBenchBatch
                Reset();
            }
            _date = tick.date;
            // ensure response is active
            if (!isValid) return;
            // ensure we are tracking active status for this symbol
            int idx = _active.addindex(tick.symbol, true);
            // if we're not active, quit
            if (!_active[idx]) return;
            // check for shutdown time
            if (tick.time > Shutdown)
            {
                // if so shutdown
                if (!_isShutDown)
                {
                    shutdown();
                }
                // and quit               
                return;
            }
            else
            {
                _isShutDown = false;
            }
            // apply bar tracking to all ticks that enter
            _kt.newTick(tick);
            _blt.newTick(tick);

            // ignore anything that is not a trade
            if (!tick.isTrade) return;
        }
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
        void shutdown()
        {
            D("shutting down everything");
            foreach (Position p in _pt)
            {
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
            _isShutDown = true;
        }

        BarInterval _barType = BarInterval.CustomTime;//"Time","Volume"
        int _numTickPerBar = 60;
        int _barsback = 16;
        int _shutdowntime = 151200;
        String _genericParameter = String.Empty;
        Boolean _useSMAFromATSGlobalIndicator = true;
        int _entrysize = 1;
        decimal _totalprofit = 0;
        int _time = 0;
        int _date = 0;
        decimal _quasiMarketOrderAdjSize = 10m;
        bool _isShutDown = false;
        ATSGlobalIndicatorPersonal.SMA _SMAFromATSGlobalIndicator = null;
        bool _black = false;
        Boolean _side = true;
        Decimal _adj = 1;

        // wait for fill
        GenericTracker<bool> _wait = new GenericTracker<bool>();
        // track whether shutdown 
        GenericTracker<bool> _active = new GenericTracker<bool>();
        // turn on bar tracking
        BarListTracker _blt = new BarListTracker();
        // turn on position tracking
        PositionTracker _pt = new PositionTracker();
        TickTracker _kt = new TickTracker();
        IdTracker _idtLocal = new IdTracker(0);

        //从外部文件读取系统/策略参数，可向Public int/decimal/string等类型的属性传值
        string _SMAExternalParam = @".\..\ControlStation\ControlParam\SMAExternalParam.csv";
        [Description("SMAExternalParam")]
        public string SMAExternalParam { get { return _SMAExternalParam; } set { _SMAExternalParam = value; } }
    }

    /// <summary>
    /// this is the same as SMAResponse, except it runs without prompting user
    /// </summary>
    public class SMAResponsePubOptReproduceAuto : SMAResponsePubOptReproduce
    {
        public SMAResponsePubOptReproduceAuto() : base(false) { }
    }
}
