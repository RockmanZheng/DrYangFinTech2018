using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATSGlobalIndicator;

namespace ATSGlobalIndicatorWrapper
{
    public class RSI : ATSGlobalIndicator.RSI, IndicatorWrapper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lookBackPeriod"></param>
        /// <param name="minimumSignalLength"></param>
        /// <param name="updatePeriod"></param>
        /// <param name="name"></param>
        public RSI(
            int lookBackPeriod = 14,
            int updatePeriod = 1,
            string name = "RSI")
        {
            _dataLength = 0;
            inUpperBand = false;
            inLowerBand = false;
            _close = 0;
            _meanUp = 0m;
            _meanDown = 0m;
            _signalVecLength = 5;
            _valueVec = new Queue<decimal>();// modify : null
            _cUpvector = new Queue<decimal>();// modify : null
            _cDownvector = new Queue<decimal>();// modify : null
            _rsi = 0m;

            Name = name;
            _lookBackPeriod = lookBackPeriod;
            _oneOver_lookBackPeriod = 1.0m / _lookBackPeriod;
            _minmumSignalLength = _lookBackPeriod;
            Period = updatePeriod;

            _calcTurningPoint = true;
            _lowerBandDownLimit = 0;
            _lowerBandUpLimit = 20;
            _upperBandDownLimit = 80;
            _upperBandUpLimit = 100;
            _turningPointValueTol = 0.0085m;
        }

        public override decimal GetSignal()
        {
            return _rsi;
        }

    }
}
