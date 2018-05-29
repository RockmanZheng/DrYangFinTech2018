using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ATSGlobalIndicator;

namespace ATSGlobalIndicatorWrapper
{
    public class SMA : ATSGlobalIndicator.SMA, IndicatorWrapper
    {
        public SMA(
            int lookBackPeriod = 14,
            int updatePeriod = 1,
            string name = "SMA")
        {
            Name = name;
            _lastSMAValue = 0;
            _isCalcStd = false;
            _valueVec = new Queue<decimal>();
            _dataLength = 0;
            _varAdj = _lookBackPerid / (_lookBackPerid - 1.0m);
            _signalVecLength = 4;
            _minmumSignalLength = lookBackPeriod;

            _lookBackPerid = lookBackPeriod;
            _alpha = 1.0m / (_lookBackPerid);
            _period = updatePeriod;
        }
    }
}
