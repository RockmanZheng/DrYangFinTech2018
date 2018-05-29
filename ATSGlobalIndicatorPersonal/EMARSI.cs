using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ATSGlobalIndicator;
using ATSGlobalIndicatorWrapper;

namespace ATSGlobalIndicatorPersonal
{
    [Serializable]
    public class EMARSI : GenericIndicatorTemplate
    {
        //int _fastEMAPeriod;
        //int _slowEMAPeriod;
        //int _RSIPeriod;

        ATSGlobalIndicatorWrapper.EMA _fastEMA;
        ATSGlobalIndicatorWrapper.EMA _slowEMA;
        ATSGlobalIndicatorWrapper.RSI _RSI;


        // Signal getter
        public decimal fastEMASignal
        {
            get { return _fastEMA.GetSignal(); }
        }
        public decimal slowEMASignal
        {
            get { return _slowEMA.GetSignal(); }
        }
        public decimal RSISignal
        {
            get { return _RSI.GetSignal(); }
        }
        public decimal EMADifference
        {
            get { return _fastEMA.GetSignal() - _slowEMA.GetSignal(); }
        }

        public decimal signal
        {
            get { return GetSignal(); }
        }

        // Initial value of weight and bias (Coefficients)
        decimal _EMA_w = 1m;
        decimal _EMA_b = 0m;
        decimal _RSI_w = -0.01m;
        decimal _RSI_b = 0.5m;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fastEMAPeriod"></param>
        /// <param name="slowEMAPeriod"></param>
        /// <param name="RSIPeriod"></param>
        public EMARSI(
            int fastEMAPeriod=14,
            int slowEMAPeriod=20,
            int RSIPeriod=14,
            decimal EMAWeight=1,
            decimal EMABias=0,
            decimal RSIWeight=-0.01m,
            decimal RSIBias=0.5m)
        {
            //_fastEMAPeriod = fastEMAPeriod;
            //_slowEMAPeriod = slowEMAPeriod;
            //_RSIPeriod = RSIPeriod;

            _fastEMA = new ATSGlobalIndicatorWrapper.EMA(lookBackPeriod: fastEMAPeriod);
            _slowEMA = new ATSGlobalIndicatorWrapper.EMA(lookBackPeriod: slowEMAPeriod);
            _RSI = new ATSGlobalIndicatorWrapper.RSI(lookBackPeriod: RSIPeriod);
            _EMA_w = EMAWeight;
            _EMA_b = EMABias;
            _RSI_w = RSIWeight;
            _RSI_b = RSIBias;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public override void UpdateValue(decimal data)
        {
            _fastEMA.UpdateValue(data);
            _slowEMA.UpdateValue(data);
            _RSI.UpdateValue(data);
        }

        /// <summary>
        /// We will optimize our coefficents using returned profit (which contains information about the current state of the market)
        /// </summary>
        /// <param name="profit"></param>
        public void UpdateCoeff(decimal profit)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override decimal GetSignal()
        {
            decimal fastEMA = _fastEMA.GetSignal();
            decimal slowEMA = _slowEMA.GetSignal();
            decimal RSI = _RSI.GetSignal();
            decimal diff = fastEMA - slowEMA;
            // Take linear combination of EMA difference and RSI to form new signal
            decimal signal = diff * _EMA_w + _EMA_b + RSI * _RSI_w + _RSI_b;
            return signal;
        }

        
    }
}
