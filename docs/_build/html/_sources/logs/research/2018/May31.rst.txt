Thu 31 May 2018
===============
Yesterday we collected a few backtesting results. We are going to visualize and analyse them here today.

And we are going to perform some sensitivity analysis.

**Note**: we choose the *best* set of parameters from calibration by its resulting Sharpe ratio. This could be used to explain why some of the mature models behave badly in gross PnL but well in Sharpe ratio.

Comparision Between Calibration And Forward Testing
---------------------------------------------------
We have used 2 sets of hyperparameters, i.e. ``popsize=maxgen=5`` and ``popsize=maxgen=50``. Here we are going to compare the results of calibration and forward testing under these 2 sets of hyperparameters, separately.

*Note*: From now on, we will call the models trained under setting ``popsize=maxgen=5`` as **young** ones, since they are trained for relatively short time. And refer those trained under parameters ``popsize=maxgen=50`` as **mature** ones, due to their longer training time.

We will be focusing on one particular objective - *Sharpe ratio*.

Young Models
^^^^^^^^^^^^
Experiments 2 and 3, corresponding date ranges

- 2015/12/15 ~ 2016/12/15
- 2016/3/15 ~ 2017/3/15

occurred something strange: performance on test set was significantly (over 90%) better than on train set. We will further analyse what has gone wrong in matrue models.

Mature Models
^^^^^^^^^^^^^
Again, testing performance was significantly better than training one. We thus want to do some sensitivity analysis on parameters over this particular date range. In particular, we want to know whether the parameters learned in training step are reasonable and generalizable. So we will single out date range that was used in calibration step, that is, 2015/12/15 ~ 2016/6/15.

With other 2 parameters fixed, we variated the remaining parameter a little bit, and run backtesting, to see the outcomes.

The result shows that all three parameters, ``LossTolerance``, ``FastSMAPeriod`` and ``PeriodDiff`` are sensitive parameters. Precisely, a little less ``LossTolerance``, we will suffer a major under-performance; a little longer ``FastSMAPeriod``, we will undertake huge amount of loss. Although the effects of the other side of variation are relatively smooth for both parameters.

For parameter ``PeriodDiff``, we have the same issue. But beside of that, we found that lowering the ``PeriodDiff`` will give us an extra bonus, which is something different from ``LossTolerance`` and ``FastSMAPeriod``, as those parameters seem to have been already optimized, but ``PeriodDiff`` is still suboptimal.

From this analysis, we could make the guess that at least during this period (2015/12/15 ~ 2016/6/15), the parameters are highly untrusted (as a small variation could lead to huge impact).


Comparision Between Young And Mature models
-------------------------------------------
Looking at the calibration results comparision plot, we see that in many cases young models could do just as good as mature ones. But in some cases, for instance, experiments 1 and 5, mature models remarkably improve young ones.

But when we look at the forward test results, we found that in most cases (6 out of 7 experiments), young models perform better or at least as good as the mature ones. This may indicate that mature models are confront the *overfitting* issue. In other words, they may be *too mature*.

In other to fix that, we could try some lower ``popsize`` and ``maxgen``, so that we could train more generalizable mature models.

Equity Curve Analysis
---------------------
As discussed above, both in young and mature models, we found something abnormal that from 2015/12/15 to 2016/12/15, the forward testing results during 2016/6/15 ~ 2016/12/15 are significantly betten than those in calibration during 2015/12/15 ~ 2016/6/15. So here we would like to further find out what has gone wrong by plotting their equity curve.

We plotted equity curves for 2 date ranges:

- Calibration: 2015/12/15 ~ 2016/6/15
- Forward Testing: 2016/6/15 ~ 2016/12/15

under the parameters of mature model.

In calibration, we obtained 2.4% of return rate in 6 months, which is roughly 5% annualized return rate. On the other hand, we undertook a -5.5% drawdown, which lasted for nerely 3 months (from December 2015 to March 2016).

In forward testing, we achieved 8% of return rate in 6 months, that is roughly 16.6% annualized return rate. However, we sufferred from a -4% drawdown beginning in August 2016, ending in November 2016, resulting a total duration of 3 months.

As can be seen, we could predict drawdown value and duration from calibration results. And fortunately, we gained a much better profit in forward testing than in calibration. This *gift*, as shall be deemed, could be due to market itself. In fact, during the first 6 months (calibration), market was experiencing a relatively high volatility (roughly 2%), while in the last 6 months (forward testing), it becomed more mild, having a relatively low volatility 0.8%.

So in summary, we may draw this conclusion:

- Under the parameters from calibration, when we are dealing with less violent market in practice, we could expect a better return rate.

So my future works may be:

- Verify that this strategy indeed has the above property, that behaves better in mild market.
- Study the performance of strategy in more violent market situation, see if it will underperform.



