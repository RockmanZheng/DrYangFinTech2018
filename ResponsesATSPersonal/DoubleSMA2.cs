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
    public class DoubleSMA2 : ResponseBase
    {
        public DoubleSMA2(bool prompt) : base(prompt) { }
        public DoubleSMA2() : this(true) { }

        ATSGlobalIndicatorWrapper.SMA _fastSMA;
        ATSGlobalIndicatorWrapper.SMA _slowSMA;
        int _FastSMAPeriod=438;
        int _PeriodDiff=423;
        int _SlowSMAPeriod;
        decimal _crossValue;
        [Description("Fast SMA Period")]
        public int FastSMAPeriod { get { return _FastSMAPeriod; } set { _FastSMAPeriod = value; } }
        [Description("SMA Period Difference")]
        public int PeriodDiff { get { return _PeriodDiff; } set { _PeriodDiff = value; } }

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
            if (!isWait)
            {
                // Short positon
                if (position < 0)
                {
                    if (_crossValue>0)
                    {
                        Buy(symbol);
                    }
                }
                else if (position > 0)// Long position
                {
                    if (_crossValue<0)
                    {
                        Sell(symbol);
                    }
                }
                else// Flat position
                {
                    if (_crossValue>0)
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
            }
        }
    }
    public class DoubleSMAAuto2 : DoubleSMA2
    {
        public DoubleSMAAuto2() : base(false) { }
    }
}
