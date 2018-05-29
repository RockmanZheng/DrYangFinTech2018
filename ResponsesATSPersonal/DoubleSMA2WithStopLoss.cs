#define USE_CLOSE
#define CAN_SHORT
#undef CAN_SHORT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;


namespace ResponsesATSPersonal
{
    public class DoubleSMA2WithStopLoss : ResponseBase
    {
        public DoubleSMA2WithStopLoss(bool prompt) : base(prompt) { }
        public DoubleSMA2WithStopLoss() : this(true) { }

        ATSGlobalIndicatorWrapper.SMA _fastSMA;
        ATSGlobalIndicatorWrapper.SMA _slowSMA;
        int _FastSMAPeriod = 299;
        int _PeriodDiff = 589;
        int _SlowSMAPeriod;
        // Maximum loss percentage we can tolerate
        decimal _LossTolerance = 4.0975m;
        // The last highest price, used to measure drawdown and loss
        decimal _lastHighPrice = 0;
        decimal _crossValue;
        decimal _lastCrossValue = 0;
        [Description("Fast SMA Period")]
        public int FastSMAPeriod { get { return _FastSMAPeriod; } set { _FastSMAPeriod = value; } }
        [Description("SMA Period Difference")]
        public int PeriodDiff { get { return _PeriodDiff; } set { _PeriodDiff = value; } }
        [Description("Loss Tolerance, in percentage")]
        public decimal LossTolerance { get { return _LossTolerance; } set { _LossTolerance = value; } }


        public override void Initialize()
        {
            _SlowSMAPeriod = _FastSMAPeriod + _PeriodDiff;
            _fastSMA = new ATSGlobalIndicatorWrapper.SMA(_FastSMAPeriod);
            _slowSMA = new ATSGlobalIndicatorWrapper.SMA(_SlowSMAPeriod);
            AddIndicator("Fast SMA", 0);
            AddIndicator("Slow SMA", 0);
        }
        public override void ResetIndicators()
        {
            _SlowSMAPeriod = _FastSMAPeriod + _PeriodDiff;
            _fastSMA = new ATSGlobalIndicatorWrapper.SMA(_FastSMAPeriod);
            _slowSMA = new ATSGlobalIndicatorWrapper.SMA(_SlowSMAPeriod);
        }

        public override void ComputeSignal()
        {
            var symbol = symbols[0];
            var lastClose = lastBars[symbol].Close;
            _fastSMA.UpdateValue(lastClose);
            _slowSMA.UpdateValue(lastClose);
            var fastSig = _fastSMA.GetSignal();
            var slowSig = _slowSMA.GetSignal();
            _crossValue = fastSig - slowSig;
            UpdateIndicator("Fast SMA", fastSig);
            UpdateIndicator("Slow SMA", slowSig);

        }

        public override void DoStrategy()
        {
            var symbol = symbols[0];
            //var signal = _fastSMA.GetSignal();
            var position = positions[symbol].Size;
            var bid = ticks[symbol].bid;
            var ask = ticks[symbol].ask;
            var close = lastBars[symbol].Close;
            var isWait = isWaitFill[symbol];
#if USE_BID_ASK
            // Short positon
            if (position < 0)
            {
                if (bid < signal)
                {
                    Buy(symbol);
                }
            }
            else if (position > 0)// Long position
            {
                if (ask > signal)
                {
                    Sell(symbol);
                }
            }
            else// Flat position
            {
                if (bid < signal)
                {
                    Buy(symbol);
                }
#if CAN_SHORT
                if (ask > signal)
                {
                    Sell(symbol);
                }
            }
#endif
#endif
#if USE_CLOSE
            // Update last high price
            _lastHighPrice = Math.Max(close, _lastHighPrice);
            if (!isWait)
            {
                // Short positon
                if (position < 0)
                {
                    if (_lastCrossValue * _crossValue <= 0 && _crossValue > 0)
                    {
                        Buy(symbol);
                    }
                }
                else if (position > 0)// Long position
                {
                    //if (_lastCrossValue*_crossValue<0&&_crossValue < 0)
                    //{
                    //    Sell(symbol);
                    //}
                    //// If loss excceeds tolerance
                    //else if (1 - close / _lastHighPrice > _LossTolerance / 100)
                    //{
                    //    Sell(symbol);
                    //    // Reset highest price
                    //    _lastHighPrice = 0;
                    //}


                    if (1 - close / _lastHighPrice > _LossTolerance / 100)
                    {
                        Sell(symbol);
                        // Reset highest price
                        _lastHighPrice = 0;
                    }
                }
                else// Flat position
                {
                    if (_lastCrossValue * _crossValue <= 0 && _crossValue > 0)
                    {
                        Buy(symbol);

                    }
#if CAN_SHORT
                else
                {
                    Sell(symbol);
                }
#endif
                }
#endif
                _lastCrossValue = _crossValue;

            }
        }
    }

    public class DoubleSMA2WithStopLossAuto : DoubleSMA2WithStopLoss
    {
        public DoubleSMA2WithStopLossAuto() : base(false) { }
    }
}
