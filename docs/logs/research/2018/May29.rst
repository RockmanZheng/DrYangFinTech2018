Tue 29 May 2018
===============

Today I will try to simulate the true environment of trading 510050.

Currently 510050 is not for trading. So I picked its related fund 001051 which is also issued by China AMC as a reference. And we will still use 510050 time series for testing our strategy.

What is T day
-------------

T day is a work day of fund organization. T days are seperated by closing of stock market, for example, from the last trading day's 15:00 to the current trading day's 15:00, this is T day. After this T day, any time before the next trading day's 15:00 is considered as in T+1 day.

How Trades Are Done
-------------------

When the reader understand the concept of T day, we can then have the following discussion about how trades are done with fund companies. 

If we tell the fund company that we want to buy 1 unit of 001051 for example on T day, then we will be buying this particular 1 unit at the closing price (at roughly 15:00) on T day. Your shares will be confirmed later by the fund company on T+1 day. You can only sell your shares after they are confirmed. 

On the other hand, similarily, if we tell the fund company that we want to sell 1 unit of 001051 on T day, then we will be selling it at the closing price. Selling will also be confirmed on T+1 day, but this will not affect our simulation, as we assume we have enough capitals and do not need to wait for money returned by fund company.