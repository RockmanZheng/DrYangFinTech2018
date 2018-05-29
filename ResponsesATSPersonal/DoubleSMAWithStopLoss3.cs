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
    public class DoubleSMAWithStopLoss3 : ResponseBase
    {
        public DoubleSMAWithStopLoss3(bool prompt) : base(prompt) { }
        public DoubleSMAWithStopLoss3() : this(true) { }

        ATSGlobalIndicatorWrapper.SMA _fastSMA;
        ATSGlobalIndicatorWrapper.SMA _slowSMA;
        int _FastSMAPeriod = 964;
        int _PeriodDiff = 421;
        int _SlowSMAPeriod;
        // Maximum loss percentage we can tolerate
        decimal _LossTolerance = 2m;
        // The last highest price, used to measure drawdown and loss
        decimal _lastHighPrice = 0;
        //decimal _crossValue;
        //decimal _lastCrossValue=0;
        decimal _crossPercent;
        decimal _lastCrossPercent = 0;

        decimal _crossPercentTolerance = 0.5m;


        [Description("Fast SMA Period")]
        public int FastSMAPeriod { get { return _FastSMAPeriod; } set { _FastSMAPeriod = value; } }
        [Description("SMA Period Difference")]
        public int PeriodDiff { get { return _PeriodDiff; } set { _PeriodDiff = value; } }
        [Description("Loss Tolerance, in percentage")]
        public decimal LossTolerance { get { return _LossTolerance; } set { _LossTolerance = value; } }
        [Description("Cross Percentage Tolerance")]
        public decimal CrossPercentTolerance { get { return _crossPercentTolerance; } set { _crossPercentTolerance = value; } }

        public override void Initialize()
        {
            _SlowSMAPeriod = _FastSMAPeriod + _PeriodDiff;
            _fastSMA = new ATSGlobalIndicatorWrapper.SMA(_FastSMAPeriod);
            _slowSMA = new ATSGlobalIndicatorWrapper.SMA(_SlowSMAPeriod);
            AddIndicator("Fast SMA", 0);
            AddIndicator("Slow SMA", 0);
            AddIndicator("Cross Percent", 0);
        }
        public override void ResetIndicators()
        {
            _SlowSMAPeriod = _FastSMAPeriod + _PeriodDiff;
            _fastSMA = new ATSGlobalIndicatorWrapper.SMA(_FastSMAPeriod);
            _slowSMA = new ATSGlobalIndicatorWrapper.SMA(_SlowSMAPeriod);
            _crossPercent = 0;
        }

        public override void ComputeSignal()
        {
            var symbol = symbols[0];
            var lastClose = lastBars[symbol].Close;
            _fastSMA.UpdateValue(lastClose);
            _slowSMA.UpdateValue(lastClose);
            var fastSig = _fastSMA.GetSignal();
            var slowSig = _slowSMA.GetSignal();
            var crossValue = fastSig - slowSig;
            _crossPercent = crossValue / slowSig * 100;
            UpdateIndicator("Fast SMA", fastSig);
            UpdateIndicator("Slow SMA", slowSig);
            UpdateIndicator("Cross Percent", _crossPercent);
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
            // Update last high price
            _lastHighPrice = Math.Max(close, _lastHighPrice);
            if (!isWait)
            {
                // Short positon
                if (position < 0)
                {
                    if (_lastCrossPercent * _crossPercent <= 0 && _crossPercent < -_crossPercentTolerance)
                    {
                        Buy(symbol);
                        // Here we use cross value, so we need to update last cross value
                        _lastCrossPercent = _crossPercent;
                    }
                }
                else if (position > 0)// Long position
                {
                    // Stop loss
                    if (1 - close / _lastHighPrice > _LossTolerance / 100)
                    {
                        Sell(symbol);
                        // Reset highest price
                        _lastHighPrice = 0;
                    }
                    else if (_lastCrossPercent * _crossPercent <= 0 && _crossPercent > _crossPercentTolerance)
                    {
                        Sell(symbol);
                        // Here we use cross value, so we need to update last cross value
                        _lastCrossPercent = _crossPercent;
                    }
                }
                else// Flat position
                {
                    if (_lastCrossPercent * _crossPercent <= 0 && _crossPercent < -_crossPercentTolerance)
                    {
                        Buy(symbol);
                        // Here we use cross value, so we need to update last cross value
                        _lastCrossPercent = _crossPercent;
                    }
                }
            }
        }
    }

    public class DoubleSMAWithStopLoss3Auto : DoubleSMAWithStopLoss3
    {
        public DoubleSMAWithStopLoss3Auto() : base(false) { }
    }
}
