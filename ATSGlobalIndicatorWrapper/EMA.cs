using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATSGlobalIndicator;

namespace ATSGlobalIndicatorWrapper
{
    public class EMA : ATSGlobalIndicator.EMA, IndicatorWrapper
    {
        /// <summary>
        /// Constructor with default values
        /// </summary>
        /// <param name="lookBackPeriod">number of data looking back when computing EMA</param>
        /// <param name="alpha">The weight of current data in computing EMA</param>
        /// <param name="minimumSignalLength">
        /// used to specify the number of data used to compute the first EMA value
        /// if this is set to 0, then we directly use the first data as the first EMA value
        /// by default, we use the first 5 data to initialize EMA
        /// </param>
        /// <param name="updatePeriod">used to specify the update period of indicator</param>
        /// <param name="name">The name of this indicator</param>
        public EMA(
            int lookBackPeriod = 14, 
            int updatePeriod = 1, 
            string name = "EMA")
        {
            // Used to store data that are used to initialize the first EMA value
            _valueVec = new Queue<decimal>();
            // "_dataLength" is used to count the number currently used in computing the first EMA value
            _dataLength = 0;
            _lastEMAValue = 0m;
            _currentEMAValue = 0m;

            _lookBackPerid = lookBackPeriod;

            _alpha = 2.0m / (_lookBackPerid + 1);
            _oneMinusAlpha = 1m - _alpha;
            _minmumSignalLength = _lookBackPerid;
            _period = updatePeriod;
            Name = name;
        }
    }
}
