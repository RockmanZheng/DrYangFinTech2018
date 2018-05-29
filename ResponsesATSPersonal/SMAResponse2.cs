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
    public class SMAResponse2 : ResponseBase
    {
        public SMAResponse2(bool prompt) : base(prompt) { }
        public SMAResponse2() : this(true) { }

        ATSGlobalIndicatorWrapper.SMA _SMA;
        int _SMAPeriod=44;
        [Description("SMA Period")]
        public int SMAPeriod { get { return _SMAPeriod; } set { _SMAPeriod = value; } }



        public override void Initialize()
        {
            _SMA = new ATSGlobalIndicatorWrapper.SMA(_SMAPeriod);
            AddIndicator("SMA", 0);
        }
        public override void ResetIndicators()
        {
            _SMA = new ATSGlobalIndicatorWrapper.SMA(_SMAPeriod);
        }

        public override void ComputeSignal()
        {
            var symbol = symbols[0];
            var lastClose = lastBars[symbol].Close;
            _SMA.UpdateValue(lastClose);

            UpdateIndicator("SMA",_SMA.GetSignal());
        }

        public override void DoStrategy()
        {
            var symbol = symbols[0];
            var signal = _SMA.GetSignal();
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
                    if (close < signal)
                    {
                        Buy(symbol);
                    }
                }
                else if (position > 0)// Long position
                {
                    if (close > signal)
                    {
                        Sell(symbol);
                    }
                }
                else// Flat position
                {
                    if (close < signal)
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
    public class SMAResponseAuto2 : SMAResponse2
    {
        public SMAResponseAuto2() : base(false) { }
    }
}
