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
using ATSGlobalIndicatorWrapper;

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
        // specifies whether the order is a buy or sell one
        Boolean _side = true;
        // Adjustment to the order
        Decimal _adj = 1;
        // 
        decimal _quasiMarketOrderAdjSize = 10m;
        // 
        int _entrysize = 1;
        // Store the last bar used in "blt_GotNewBar"s
        Bar _lastBar;
        Bar _prevBar;
        // average period
        int _period = 14;
        // Instance of ATR indicator
        ATSGlobalIndicatorPersonal.ATR _ATR = null;
        // Instance of EMA indicator
        ATSGlobalIndicatorWrapper.EMA _EMA = null;

        // Indicator titles
        // These titles will be shown in the first line of the table in "indicators" panel
        string[] _indicators = new string[] { "Time", "EMA", "ATR[‰]", "LastClose" };

        /// <summary>
        /// Construct indicators for display
        /// </summary>
        /// <returns></returns>
        string[] GetDisplayIndicators()
        {
            string[] indicators = new string[]{
                _time.ToString(), 
                _EMA.GetSignal().ToString("F5",System.Globalization.CultureInfo.InvariantCulture),
                (_ATR.GetSignal()*1000).ToString("F5", System.Globalization.CultureInfo.InvariantCulture), 
                _lastBar.Close.ToString("F5", System.Globalization.CultureInfo.InvariantCulture) 
            };
            return indicators;
        }
        /// <summary>
        /// Compute signal for determining response
        /// </summary>
        /// <param name="lastBar"></param>
        /// <returns></returns>
        decimal ComputeSignal(Bar lastBar)
        {
            _EMA.UpdateValue(lastBar.Close);
            // calculate the ATR using closing prices for so many bars back
            _ATR.UpdateValue(lastBar.High, lastBar.Low, lastBar.Close, _EMA.GetSignal());
            return _ATR.GetSignal();
        }
        /// <summary>
        /// Implementation needed
        /// </summary>
        /// <param name="tOther"></param>
        /// <param name="signal"></param>
        void DoStrategy(string symbol, decimal signal)
        {
        }
        /// <summary>
        /// Initialize all needed indicators
        /// </summary>
        void Initialize_Indicators()
        {
            _ATR = new ATSGlobalIndicatorPersonal.ATR(_period);
            _EMA = new ATSGlobalIndicatorWrapper.EMA(_period);
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
        [Description("Shutdown time")]
        public int Shutdown { get { return _shutdowntime; } set { _shutdowntime = value; } }
        [Description("Entry size when signal is found")]
        public int EntrySize { get { return _entrysize; } set { _entrysize = value; } }
        [Description("Bars back when calculating EMA and atr")]
        public int Period { get { return _period; } set { _period = value; } }

        /// <summary>
        /// This method will be delegated in constructor method "EMARSIResponsePub(bool prompt)" to "_active" tracker
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
            Indicators = _indicators;
        }

        /// <summary>
        /// Default constructor. The default settting is enabling prompting of parameters
        /// </summary>
        public ATRResponsePub() : this(true) { }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="tOther"></param>
        /// <param name="message"></param>
        void Buy(string symbol, string message = null)
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
            LimitOrder lOrder = new LimitOrder(symbol, _side, EntrySize, tOther.trade - _adj, _orderidLocal);
            sendorder(lOrder);
            // wait for fill
            _wait[symbol] = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="tOther"></param>
        /// <param name="message"></param>
        void Sell(string symbol, string message = null)
        {
            Tick tOther = _kt[symbol];

            if (message != null)
            {
                D(message);
            }
            _side = false;
            _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
            Int64 _orderidLocal = _idtLocal.AssignId;
            LimitOrder lOrder = new LimitOrder(symbol, _side, EntrySize, tOther.trade - _adj, _orderidLocal);
            sendorder(lOrder);
            // wait for fill
            _wait[symbol] = true;
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
            // If we only get less than 2 bars, exit
            if (myBars.Count <= 1) return;
            // make sure the lastBar is full
            _lastBar = myBars[myBars.Count - 2];
            //_prevBar = myBars[myBars.Count - 3];
            
            decimal signal = ComputeSignal(_lastBar);
            string[] indicators = GetDisplayIndicators();

            sendindicators(indicators);
            // Buy or sell
            // Strategy based on computed ATR indicator
            DoStrategy(symbol, signal);
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

            Initialize_Indicators();
            // set to false to indicate the system is on
            _isShutDown = false;
            // clear total profit
            _totalprofit = 0m;
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
                _totalprofit = pl;
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
    /// this is the same as base class response, except it runs without prompting user parameters setting interface
    /// This response will be used in "AutoRun" mode when we are processing multiple tick data files
    /// </summary>
    public class ATRResponsePubAuto : ATRResponsePub
    {
        public ATRResponsePubAuto() : base(false) { }
    }
}
