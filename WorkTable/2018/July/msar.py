"""
Use Markov auto-regression model to analyse daily close price series
"""
import numpy as np
import pandas as pd
import os
from datetime import date,datetime
import matplotlib.pyplot as plt
from pdb import set_trace
from statsmodels.tsa.regime_switching.markov_autoregression import MarkovAutoregression

f = open("Daily.csv")
df = pd.read_csv(f)
f.close()
dates = df["Date"].apply(lambda x:datetime.strptime(str(x),"%Y-%m-%d"))
# set_trace()
y = pd.Series(np.array(df["Close"]),index=pd.DatetimeIndex(dates))
# y = (y/y[0]-1)
diff = np.array(y.diff())
y_a = np.array(y)
y = diff[1:]/y_a[0:len(y_a)-1]*100
y = pd.Series(y,index=pd.DatetimeIndex(dates[1:]))
plt.subplot(2,1,1)
# y = y-y.mean()
y.plot(title="Index Future Returns")
plt.subplot(2,1,2)
plt.acorr(y)
plt.show()

regimes_num = 3

model = MarkovAutoregression(y,k_regimes=regimes_num,order=1,trend="nc",switching_ar=True,switching_variance=True)
result = model.fit()
print(result.summary())

low_vola = np.array(result.smoothed_marginal_probabilities[0]+result.smoothed_marginal_probabilities[1])
low_vola = pd.Series(low_vola,index=pd.DatetimeIndex(dates[1:len(dates)-1]))

high_vola = np.array(result.smoothed_marginal_probabilities[2])
high_vola = pd.Series(high_vola,index=pd.DatetimeIndex(dates[1:len(dates)-1]))

plt.subplot(2,1,1)
low_vola.plot(title="Smoothed Probabilities of Low Volatility Regime")
plt.subplot(2,1,2)
high_vola.plot(title="Smoothed Probabilities of High Volatility Regime")
plt.show()