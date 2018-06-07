using System;
using System.Collections;
using System.Collections.Generic;
using TradeLink.Common;
using TradeLink.API;
using System.ComponentModel;
using System.Reflection;
using CommonUtilityAlbert;
using CommonUtilityAlbertA;
using ATSGlobalIndicator;
using ATSGlobalIndicatorPersonal;

namespace ResponsesATSPersonal
{
    public class DoubleSMA510050ResponsePub : ResponseTemplateBasePub
    {
        decimal _LastShortSMA = decimal.MinValue;
        decimal _LastLongSMA = decimal.MinValue;
        int _currentEntry = 0;                           //多头+1，空头-1，记录净交易
        decimal _highestprice = 0m;                       //使用ATR止损，记录最高价
        decimal _highestcloseprice = 0m;                  //记录最高收盘价
        BarInterval _barType = BarInterval.CustomTime;//"Time","Volume"    CustomTime是K线的意思，这个time就是周期，会进入GOTNEWBAR
        int _numTickPerBar = 60;       // K线的周期，按秒计算
        int _shortbarsback =5 ;            //SMA的特征值
        int _incrementbarsback = 15;
        int _shutdowntime = 151200;    //全部清仓的时间，代表15点12分
        String _genericParameter = String.Empty;
        Boolean _useSMAFromATSGlobalIndicator = true;
        int _entrysize = 1;
        decimal _totalprofit = 0;
        int _time = 0;
        int _date = 0;
        decimal _quasiMarketOrderAdjSize = 10m;
        bool _isShutDown = false;
        ATSGlobalIndicatorPersonal.SMA _ShortSMAFromATSGlobalIndicator = null;
        ATSGlobalIndicatorPersonal.SMA _LongSMAFromATSGlobalIndicator = null;
        ATSGlobalIndicatorPersonal.ATR _ATRFromATSGlobalIndicator = null;        //调用ATR添加进的系统变量
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


        //indicator还需补充
        [Description("BarType")]
        public BarInterval BarType { get { return _barType; } set { _barType = value; } }
        [Description("NumItemPerBar")]
        public int NumItemPerBar { get { return _numTickPerBar; } set { _numTickPerBar = value; } }
        [Description("Short Bars back when calculating sma")]
        public int ShortBarsBack { get { return _shortbarsback; } set { _shortbarsback = value; } }
        [Description("Increment Bars back when calculating sma")]
        public int IncrementBarsBack { get { return _incrementbarsback; } set { _incrementbarsback = value; } }
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

        public DoubleSMA510050ResponsePub() : this(true) { }
        public DoubleSMA510050ResponsePub(bool prompt)
        {
            _black = !prompt;
            // handle when new symbols are added to the active tracker
            _active.NewTxt += new TextIdxDelegate(_active_NewTxt);

            // set our indicator names, in case we import indicators into R
            // or excel, or we want to view them in gauntlet or kadina
            Indicators = new string[] { "Time", "Short_SMA","Long_SMA", "LastClose" };   
        }

        public override void Reset()     //对于新的一天来计算的时候要进行重置
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

            GenericParamUpdateHelper.updateParam(this, GenericParameter, false);

            Hashtable ShortSMAInputParamTmp = new Hashtable();   //对于SMA indicator进行更新，用今天重新计算的SMA，而不用昨天
            Hashtable LongSMAInputParamTmp = new Hashtable();
            ShortSMAInputParamTmp["lookbackperiod"] = _shortbarsback;
            LongSMAInputParamTmp["lookbackperiod"] = _shortbarsback + _incrementbarsback;
            ShortSMAInputParamTmp["minmumsignallength"] = _shortbarsback;
            LongSMAInputParamTmp["minmumsignallength"] = _shortbarsback+_incrementbarsback;
            _ShortSMAFromATSGlobalIndicator = new ATSGlobalIndicatorPersonal.SMA();
            _LongSMAFromATSGlobalIndicator = new ATSGlobalIndicatorPersonal.SMA();
            _ShortSMAFromATSGlobalIndicator.Param = ShortSMAInputParamTmp;
            _LongSMAFromATSGlobalIndicator.Param = LongSMAInputParamTmp;
            _ShortSMAFromATSGlobalIndicator.Initializatin();
            _LongSMAFromATSGlobalIndicator.Initializatin();
            // reset the parameters of ATR
            Hashtable ATRInputParamTmp = new Hashtable();
            ATRInputParamTmp["lookbackperiod"] = _shortbarsback;
            ATRInputParamTmp["minmumsignallength"] = 14;
            _ATRFromATSGlobalIndicator = new ATSGlobalIndicatorPersonal.ATR();
            _ATRFromATSGlobalIndicator.Param = ATRInputParamTmp;
            _ATRFromATSGlobalIndicator.Initializatin();
            //---------------------------------------------------------
            _isShutDown = false;
            TotalProfit = 0m;
            _pt.Clear();
            if (_shortbarsback != 34 || _incrementbarsback != 84)
            {
                Console.WriteLine("yo hey");
            }
        }

        void _active_NewTxt(string txt, int idx)      //照抄
        {
            // go ahead and notify any other trackers about this symbol
            _wait.addindex(txt, false);
        }

        void blt_GotNewBar(string symbol, int interval)       //K线的周期是可以自己定的，在Gottick中累计到周期后，就会自动进入GotNewBar中
        {
            //symbol是合约名字
            int idx = _active.getindex(symbol);  //系统变量，一般是1
            Tick tOther = _kt[symbol];  //取到最新的tick，包含成交价，买1价，卖1价，买卖量等等

            // calculate the SMA using closign prices for so many bars back
            decimal ShortSMA = decimal.MinValue;
            decimal LongSMA = decimal.MinValue;
            BarList _myBars = _blt[symbol, interval];  //这一天所有的K线
            // make sure the lastBar is full
            Bar _lastBar = _myBars[_myBars.Count - 2];  //取倒数第2个，一个是因为零索引，还有一个就是比如现在是10点，
            //那么在10点零1分的第1秒组成了10点钟的K线，所以要往回取一个
            //-----------调用ATR独有的部分------------------------------
            decimal ATR = decimal.MinValue;
            Bar _previousBar = _myBars[_myBars.Count - 3];  // 前一个bar
            //----------------------------------------------------------
            bool isReverse = false;           //false
            if (UseSMAFromATSGlobalIndicator)
            {
                _ShortSMAFromATSGlobalIndicator.UpdateValue(_lastBar.Close);   //UpdateValue是连接response和indicator的重要函数
                //通过UpdateValue把昨天的收盘价传给Indicator
                ShortSMA = _ShortSMAFromATSGlobalIndicator.GetSignal();   //把计算好的SMA传回Response中
                _LongSMAFromATSGlobalIndicator.UpdateValue(_lastBar.Close);   //UpdateValue是连接response和indicator的重要函数
                //通过UpdateValue把昨天的收盘价传给Indicator
                LongSMA = _LongSMAFromATSGlobalIndicator.GetSignal();   //把计算好的SMA传回Response中
                //-----------------------------
                decimal firstRange = _lastBar.High - _lastBar.Low;
                decimal secondRange = Math.Abs(_lastBar.High - _previousBar.Close);
                decimal thirdRange = Math.Abs(_lastBar.Low - _previousBar.Close);
                decimal TR = Math.Max(firstRange, Math.Max(secondRange, thirdRange));

                // deal with some outlier
                if (firstRange < 0) { return; }

                _ATRFromATSGlobalIndicator.UpdateValue(TR);          // update ATR value
                ATR = _ATRFromATSGlobalIndicator.GetSignal();        // get the updated ATR value
                //--------------------------------
            }
            else
            {
                ShortSMA = Calc.Avg(Calc.EndSlice(_blt[symbol].Open(), _shortbarsback));
                LongSMA = Calc.Avg(Calc.EndSlice(_blt[symbol].Open(), _shortbarsback+_incrementbarsback));
            }
            // wait until we have an SMA
            if (ShortSMA == 0||LongSMA==0)
                return;

            if (_LastLongSMA == decimal.MinValue || _LastShortSMA == decimal.MinValue)
            {
                _LastShortSMA = ShortSMA;
                _LastLongSMA = LongSMA;
                return;
            }

            //---------------给highestprice赋值--------
            if (_highestprice < _lastBar.High)
            {
                _highestprice = _lastBar.High;
            }
            if(_highestcloseprice<_lastBar.Close)
            {
                _highestcloseprice=_lastBar.Close;
            }
            //-----------------------------------------

            //ensure we aren't waiting for previous order to fill
            if (!_wait[symbol])  //如果有挂单，不要再发信号
            {
                // if we're flat and not waiting
                if (_pt[symbol].isFlat)                      //空仓
                {
                    // if our current price is above SMA, buy
                    if (_LastShortSMA<_LastLongSMA&&ShortSMA>LongSMA)    
                    {
                        D("Short SMA crosses above Long SMA , buy");
                        //sendorder(new BuyMarket(symbol, EntrySize));
                        _side = true;                                          //true就是买入
                        _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;   //为了达成成交需要的一串代码，先照抄，以后再理解
                        Int64 _orderidLocal = _idtLocal.AssignId;              //记录orderid 照抄
                        LimitOrder lOrder = new LimitOrder(symbol, _side, EntrySize, tOther.trade - _adj, _orderidLocal); //生成这一个订单，entrysize是下单量，t是价格
                        sendorder(lOrder);  //把订单发出去         系统的空仓和平仓是一样的，如果想从多仓1手变成空仓1手，entrysize乘2就好了 
                        // wait for fill
                        _wait[symbol] = true;   //记录一下这个单已经发出去了
                        _currentEntry += 1;
                    }
                    // otherwise if it's less than SMA, sell
                    if (_LastShortSMA > _LastLongSMA && ShortSMA < LongSMA)
                    {
                        D("Long SMA crosses above Short SMA , sell");
                        //sendorder(new SellMarket(symbol, EntrySize));
                        _side = false;                                       //和上面相反
                        _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
                        Int64 _orderidLocal = _idtLocal.AssignId;
                        LimitOrder lOrder = new LimitOrder(symbol, _side, EntrySize, tOther.trade - _adj, _orderidLocal);
                        sendorder(lOrder);
                        // wait for fill
                        _wait[symbol] = true;
                        _currentEntry -= 1;
                    }
                }
                else if ((_pt[symbol].isLong && (_LastShortSMA > _LastLongSMA && ShortSMA < LongSMA))
                    || (_pt[symbol].isShort && (_LastShortSMA < _LastLongSMA && ShortSMA > LongSMA)))     
                {
                    int reverse = 1;
                    if (isReverse) 
                    {
                        reverse = Math.Abs(_currentEntry)*2;  //等于1的话恢复算法
                    }
                    D("counter trend, exit.");
                    //sendorder(new MarketOrderFlat(pt[symbol]));
                    _side = !_pt[symbol].isLong;
                    _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
                    Int64 _orderidLocal = _idtLocal.AssignId;
                    LimitOrder lOrder = new LimitOrder(symbol, _side, EntrySize*reverse, tOther.trade - _adj, _orderidLocal);
                    sendorder(lOrder);
                    // wait for fill
                    _wait[symbol] = true;
                    _currentEntry = -_currentEntry;
                }
               //-------------------综合使用YOYO止损法和吊灯止损法--------------------
              /* if ((!_pt[symbol].isFlat && _lastBar.Close < _highestprice - 3 * ATR) 
                   || (!_pt[symbol].isFlat && _lastBar.Close < _highestcloseprice - (decimal)2.5 * ATR) || 
                    (!_pt[symbol].isFlat && _lastBar.Close < _previousBar.Close - (decimal)2* ATR))
                {
                    int stop = Math.Abs(_currentEntry);
                    D("It's time to stop.");
                    _side = !_pt[symbol].isLong;
                    _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
                    Int64 _orderidLocal = _idtLocal.AssignId;
                    LimitOrder lOrder = new LimitOrder(symbol, _side, EntrySize * stop, tOther.trade - _adj, _orderidLocal);
                    sendorder(lOrder);
                    // wait for fill
                    _wait[symbol] = true;
                    _currentEntry = 0;
                }*/
                //------------------------------------------------------------------
            }

            // this way we can debug our indicators during development
            // indicators are sent in the same order as they are named above
            sendindicators(new string[] { _time.ToString(), 
                                          ShortSMA.ToString("F5", System.Globalization.CultureInfo.InvariantCulture), 
                                          LongSMA.ToString("F5", System.Globalization.CultureInfo.InvariantCulture),
                                          _lastBar.Close.ToString("F5", System.Globalization.CultureInfo.InvariantCulture)});
                         //打印在bench的indicator中
            _LastLongSMA = LongSMA;
            _LastShortSMA = ShortSMA;
        }

        public override void GotTick(Tick tick)           //数据进来，先进gottick，如果需要对成交价等进行更改，在gottick里进行
        {
            // keep track of time from tick

            _time = tick.time;
            if (_date > tick.date)
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
            // apply bar tracking to all ticks that enter
            _kt.newTick(tick);
            _blt.newTick(tick);

            // ignore anything that is not a trade
            if (!tick.isTrade) return;       //如果没有trade，这个数据是用不到的
        }

        public override void GotFill(Trade fill)       //单子成交会进入GotFill中
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

        void shutdown()          //系统不能留隔夜仓，调用shutdown来平掉全部仓，以确保没有隔夜仓
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

       
    }

    /// <summary>
    /// this is the same as DoubleSMAResponse, except it runs without prompting user
    /// </summary>
    public class DoubleSMA510050ResponseAutoPub : DoubleSMA510050ResponsePub
    {
        public DoubleSMA510050ResponseAutoPub() : base(false) { }
    }


}

