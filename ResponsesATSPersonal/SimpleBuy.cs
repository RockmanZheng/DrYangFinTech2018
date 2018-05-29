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
    public class SimpleBuy : ResponseBase
    {
        public SimpleBuy(bool prompt) : base(prompt) { }
        public SimpleBuy() : this(true) { }

        public override void Initialize()
        {
        }
        public override void ResetIndicators()
        {
        }

        public override void ComputeSignal()
        {
        }

        public override void DoStrategy()
        {
            var symbol = symbols[0];
            var isWait = isWaitFill[symbol];
            var position = positions[symbol].Size;

            if (!isWait)
            {
                // Short positon
                if (position < 0)
                {
                }
                else if (position > 0)// Long position
                {

                }
                else// Flat position
                {
                    Buy(symbol);
                }
            }
        }
    }
    public class SimpleBuyAuto : SimpleBuy
    {
        public SimpleBuyAuto() : base(false) { }
    }
}
