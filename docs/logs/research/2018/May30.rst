Wed 30 May 2018
===============

This morning I had a discussion with Jia Yang about how 510050 is traded. He told me that the tick data that we collected about 510050 was from some secondary market, where 510050 is traded just like stock. What I had found yesterday in this article :doc:`May29`, only applies to primary market, that is, only valid if we are trading directly with fund company. In secondary market, we trade this fund like a stock with other traders on an exchange platform for instance. 

Using the terms in yesterday's doc, trades are confirmed in T+0 way in secondary market. In other words, if you buy 1 share of 510050 in this market on T day, it is confirmed right away, but only allows to be sold on T+1 day. Just to compare, in primary market, you apply for buying 1 share of 510050 on T day, the fund company confirms it on T+1 day, thus you are only allowed to sell it after T+2 day inclusively.

How To Trade Intradaily
-----------------------
In order to facilitate intraday trading, we need a little trick. On the first day we enter market, we buy 1 share of 510050 as our bottom position, and hold it for 1 day (no selling this day). Under the assumption that we only buy or sell at most 1 share and no shorting is permitted, in later trading day we could perform buying and selling in intraday way. This is because everytime we sell 1 share, we are selling the one bought on some earlier day, which is legitamate.

So if we set bottom position as 1 share, and only buy or sell 1 share each time, we can at most sell 1 time a day. On the contrary, buying times in unlimited.

Experiments and Coding
----------------------
I am going to perfrom 2 sets of experiments. One for intraday trading and the other on for interday trading. Then we are going to compare the results.

Intraday Trading - Experiment 01
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
**Coding**

- Made a copy of *ResponsesATSPersonal/DoubleSMA2WithStopLoss.cs* and change it into *ResponsesATSPersonal/DSMAWithStopLossIntraday.cs*. 
- Added boolean variable ``_isOpenBottomPosition`` to indicate whether we have open bottom position yet, which is initialized as ``false``.
- Added integer variable ``_openBottomPositionDate`` to record the date we open bottom position.
- Added integer variable ``_lastSellDate`` to record the date last time we sell. So that we can limit sell times to 1 each day.
- In ``DoStrategy`` method, added 2 judge statements to facilitate opening bottom position on the first day we enter market.
- In ``DoStrategy`` method, we classify ``position==1`` as flat position, as we should not touch the bottom position if we want to perform intraday trading.
- Fixed bug about resetting. In *ResponsesATSPersonal/ResponseInterface.cs* added interface ``LocalReset``, and add corresponding abstract function in *ResponsesATSPersonal/ResponseBase.cs*, and call it in ``Reset`` method.
- Added ``LocalRest`` method in *ResponsesATSPersonal/DSMAWithStopLossIntraday.cs* to perform resetting work.

**Parameters Control**

- Made copies of *ControlParam/Control510050DoubleSMA2WithStopLoss.csv* and *ControlParam/OptParam510050DoubleSMA2WithStopLoss.csv* and change them into *ControlParam/Control510050DSMAWithStopLossIntraday.csv* and *ControlParam/OptParam510050DSMAWithStopLossIntraday.csv*.
- In *ControlParam/Control510050DSMAWithStopLossIntraday.csv*, changed values under ``responsename`` into ``ResponsesATSPersonal.DSMAWithStopLossIntradayAuto``, values under ``runparamfile`` into ``OptParam510050DSMAWithStopLossIntraday.csv``, so that we can use strategy created above when running *ATSTestBenchBatch.exe*

**Experiment Setup**

Date Range:

- Train: 15 Sep 2015 to 15 Sep 2016
- Test: 15 Sep 2016 to 15 Sep 2017

Hyperparameters (as in *ControlParam/OptParam510050DSMAWithStopLossIntraday.csv*):

- ``popsize``: 25
- ``maxgen``: 25
- ``LossTolerance``: 0.01~1
- ``FastSMAPeriod``: 1~120
- ``PeriodDiff``: 1~120

**Steps**

- We copy all trades into Excel. Calculate cash we currently have and shares we hold.

**Results**

Optimized Parameters:

- ``LossTolerance``: 0.8253 [%]
- ``FastSMAPeriod``: 53 [bar]
- ``PeriodDiff``: 62 [bar]

*Note*: We group 60 ticks into 1 bar.

Performance on 510050 (train):

- GrossPL: 0.549
- AvgPerTrade: 0.002
- SharpeRatio: 1.5
- MaxDDVal: -0.677

Performance on 510050 (test):

- GrossPL: 0.398
- AvgPerTrade: 0.005
- MaxDDVal: -0.091
- Trades: 75
- SharpeRatio: 4.9
- SortinoRatio: 17

Question: Why is performance on test set better than train set?

Intraday Trading - Experiment 02
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
From calculation in Excel, we found out each time we start a new backtest, the system is reset, and it will open a new bottom position. We do not want to see it accumulatingly create bottom position. So to amend this, we just drop this step, simply assuming we do have bottom position, but not realizing it in our coding:

**Coding**

- Made a copy of *ResponsesATSPersonal/DSMAWithStopLossIntraday.cs* and change it into *ResponsesATSPersonal/DSMAWithStopLossIntraday2.cs*. 
- Deleted anything related to bottom position mechanism.

**Parameters Control**

- Made copies of *ControlParam/Control510050DSMAWithStopLossIntraday.csv* and *ControlParam/OptParam510050DSMAWithStopLossIntraday.csv* and change them into *ControlParam/Control510050DSMAWithStopLossIntraday2.csv* and *ControlParam/OptParam510050DSMAWithStopLossIntraday2.csv*.
- In *ControlParam/Control510050DSMAWithStopLossIntraday2.csv*, changed values under ``responsename`` into ``ResponsesATSPersonal.DSMAWithStopLossIntraday2Auto``, values under ``runparamfile`` into ``OptParam510050DSMAWithStopLossIntraday2.csv``, so that we can use strategy created above when running *ATSTestBenchBatch.exe*

**Experiment Setup**

Date Range:

- Train: 15 Sep 2015 to 15 Sep 2016
- Test: 15 Sep 2016 to 15 Sep 2017

Hyperparameters (as in *ControlParam/OptParam510050DSMAWithStopLossIntraday2.csv*):

- ``popsize``: 25
- ``maxgen``: 25
- ``LossTolerance``: 0.01~1
- ``FastSMAPeriod``: 1~120
- ``PeriodDiff``: 1~120

**Basic Results**

Optimized Parameters:

- ``LossTolerance``: 0.88 [%]
- ``FastSMAPeriod``: 31 [bar]
- ``PeriodDiff``: 45 [bar]

*Note*: We group 60 ticks into 1 bar.

Performance on 510050 (train):

- GrossPL: 0.54
- AvgPerTrade: 0.002
- SharpeRatio: 1.8
- MaxDDVal: -0.168

Performance on 510050 (test):

- GrossPL: 0.161
- AvgPerTrade: 0.002
- SharpeRatio: 2.5
- MaxDDVal: -0.077
- Trades: 70
- SortinoRatio: 8.6

**Analysis Steps and Results**

First we need to confirm the validity of calibration results. We run *ATSTestBench.exe* and load the same date range of data and the same strategy. In *Results* panel we will check its consistency with figures given in *ATSTestBenchBatch.exe* run earlier. The result is OK.

Then we need to validate the backtesting results in similar approach. Also no problem.

We then need to further look at the equity curves of calibration and backtesting. A rough equity curve of backtesing is already given by *ATSTestBenchBatch.exe* in its *Equity* panel. But we want to plot our own curves with other tools to verify and manipulate them. Plus they are not so hard to plot ourselves.

Intraday Trading - Experiment 03
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
I suddenly found out that I have been using *ATSTestBenchBatch.exe* in the wrong way. For newcomer of this project, you should check out the demonstration video *Demo/TVVideo/8. TestBenchBatch_Forward Test Demo.tvs* to better learn how to use it. For outsider, you can learn the concept of forward testing on this `website <http://www.amibroker.com/guide/h_walkforward.html>`_.

So now after a few experiments, I decided to make the system do calibration on 8-month basis, and do forward testing on 2-month basis. More specifically, I will list all calibration and forward testing circle in the following figure as:

First I used rather small ``popsize`` and ``maxgen`` for initial run.

The calibration and forward testing results under these parameters are visualized as:


