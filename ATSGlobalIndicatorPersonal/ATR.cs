using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ATSGlobalIndicator;
using TradeLink.API;

namespace ATSGlobalIndicatorPersonal
{
    [Serializable]
    public class ATR : GenericIndicatorTemplate
    {
        /* Developed by J. Welles Wilder, the Average True Range (ATR) is an indicator that measures volatility. 
         * As with most of his indicators, Wilder designed ATR with commodities and daily prices in mind. 
         * Commodities are frequently more volatile than stocks. 
         * They were are often subject to gaps and limit moves, which occur when a commodity opens up or down its maximum allowed move for the session. 
         * A volatility formula based only on the high-low range would fail to capture volatility from gap or limit moves. 
         * Wilder created Average True Range to capture this “missing” volatility. 
         * It is important to remember that ATR does not provide an indication of price direction, just volatility. 
         * Wilder features ATR in his 1978 book, New Concepts in Technical Trading Systems. 
         * This book also includes the Parabolic SAR, RSI and the Directional Movement Concept (ADX). 
         * Despite being developed before the computer age, Wilder's indicators have stood the test of time and remain extremely popular. 
         * 
         * Check out the website http://stockcharts.com/school/doku.php?st=atr&id=chart_school:technical_indicators:average_true_range_atr for more information.
         */

        // Store close price of the previous bar
        decimal _previousClose = decimal.MinValue;
        // indicate whether we have computed the first true range
        bool _isFirstTRComputed = false;
        // Current absolute average true range value
        decimal _currentAbsATR = 0m;
        // Current relative ATR value
        decimal _currentRelATR = 0m;
        // number of bars that are used to compute ATR
        int _period = 14;
        // count how many times we have update ATR
        int _countUpdate = 0;

        public ATR(int period = 14)
        {
            _period = period;
        }

        /// <summary>
        /// Initializatin
        /// </summary>
        public override void Initializatin()
        {
            base.Initializatin();
        }
        /// <summary>
        /// Getting parameter from input
        /// </summary>
        public override void GetParam()
        {
            base.GetParam();
            if (_inputParam.ContainsKey("period"))
            {
                _period = Convert.ToInt32(_inputParam["period"].ToString());
            }
        }

        /// <summary>
        /// Compute true range value
        /// </summary>
        /// <param name="high"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        decimal ComputeTR(decimal high, decimal low)
        {
            // if this is not the first true range value
            if (_isFirstTRComputed)
            {
                decimal tmp1 = Math.Abs(high - low);
                decimal tmp2 = Math.Abs(high - _previousClose);
                decimal tmp3 = Math.Abs(low - _previousClose);
                tmp1 = Math.Max(tmp1, tmp2);
                return Math.Max(tmp1, tmp3);
            }
            else
            {
                // This is the first true range value
                _isFirstTRComputed = true;
                return Math.Abs(high - low);
            }
        }

        /// <summary>
        /// Use current bar data to update ATR indicator
        /// </summary>
        /// <param name="currentBar"></param>
        public void UpdateValue(decimal high, decimal low, decimal close, decimal currentSMA)
        {
            if (currentSMA == 0m)
            {
                _currentRelATR = 0m;
            }
            else
            {

                decimal TR = ComputeTR(high, low);
                _previousClose = close;
                // If we have not yet computed "_period" times of TR
                if (_countUpdate < _period)
                {
                    _currentAbsATR = (_currentAbsATR * _countUpdate + TR) / (_countUpdate + 1);
                    _countUpdate++;
                }
                else
                {
                    // If we have computed "_period" times of TR
                    _currentAbsATR = (_currentAbsATR * (_period - 1) + TR) / _period;
                }
                _currentRelATR = _currentAbsATR / currentSMA;
            }
        }
        /// <summary>
        /// Signal value output
        /// </summary>
        public override Decimal GetSignal()
        {
            return _currentRelATR;
        }

    }
}
