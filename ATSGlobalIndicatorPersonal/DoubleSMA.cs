using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ATSGlobalIndicator;
using ATSGlobalIndicatorWrapper;

namespace ATSGlobalIndicatorPersonal
{
    [Serializable]
    public class DoubleSMA : GenericIndicatorTemplate
    {
        ATSGlobalIndicatorWrapper.SMA _fastSMA;
        ATSGlobalIndicatorWrapper.SMA _slowSMA;

        // Signal getter
        public decimal fastSMASignal
        {
            get { return _fastSMA.GetSignal(); }
        }
        public decimal slowSMASignal
        {
            get { return _slowSMA.GetSignal(); }
        }
        public decimal SMADifference
        {
            get { return _fastSMA.GetSignal() - _slowSMA.GetSignal(); }
        }

        public decimal signal
        {
            get { return GetSignal(); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fastSMAPeriod"></param>
        /// <param name="slowSMAPeriod"></param>
        public DoubleSMA(
            int fastSMAPeriod=14,
            int slowSMAPeriod=20)
        {
            _fastSMA = new ATSGlobalIndicatorWrapper.SMA(lookBackPeriod: fastSMAPeriod);
            _slowSMA = new ATSGlobalIndicatorWrapper.SMA(lookBackPeriod: slowSMAPeriod);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public override void UpdateValue(decimal data)
        {
            _fastSMA.UpdateValue(data);
            _slowSMA.UpdateValue(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override decimal GetSignal()
        {
            decimal fastSMA = _fastSMA.GetSignal();
            decimal slowSMA = _slowSMA.GetSignal();
            decimal diff = fastSMA - slowSMA;
            return diff;
        }

        
    }
}
