using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Trady.Core;
using System.Linq;
using Kite.AutoTrading.StrategyManager.Strategy;
using KiteConnect;
using System.Threading.Tasks;
using Kite.AutoTrading.Data.DataServices;
using Kite.AutoTrading.Data.EF;
using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Business.Brokers;
using Kite.AutoTrading.Common.Utility;
using Kite.Autotrading.Test.Helpers;

namespace Kite.Autotrading.Test
{
    [TestClass]
    public class MAStrategyTest
    {
        [TestMethod]
        public async Task ScriptIsProfitableOrNot()
        {
            //Get All Candles according to
            MAStrategy mAStrategy = new MAStrategy();
            var symbolService = new SymbolService();
            var userSessionService = new UserSessionService();
            var symbol = await symbolService.GetAsync(21108);
            var userSession = await userSessionService.GetCurrentSession();

            PositionTestModel positionTestModel = null;

            if (userSession != null)
            {
                var zeropdhaService = new ZerodhaBroker(new UserSessionModel()
                {
                    AccessToken = userSession.AccessToken,
                    ApiKey = userSession.ApiKey,
                    AppSecret = userSession.AppSecret,
                    PublicToken = userSession.PublicToken,
                    UserId = userSession.UserId
                });
                var candles = zeropdhaService.GetData(
                                    symbol, mAStrategy._HistoricalDataTimeframe,
                                    DateTime.Now.AddDays(-7),
                                    DateTime.Now.AddDays(-2), false);
                positionTestModel = GetProfitOfDay(symbol, candles);

            }
            Assert.IsTrue(positionTestModel.Profit > 1, "Symbol is Not Profitable");
        }

        [TestMethod]
        public async Task WatchlistIsProfitableOrNot()
        {
            List<PositionTestModel> positionTestModels = new List<PositionTestModel>();
            //Get All Candles according to
            MAStrategy mAStrategy = new MAStrategy();
            var watchlistService = new WatchlistService();
            var userSessionService = new UserSessionService();

            var symbols = await watchlistService.GetSymbols(1);
            var userSession = await userSessionService.GetCurrentSession();

            if (userSession != null)
            {
                Parallel.ForEach(symbols.Take(50), symbol =>
                {
                    var zeropdhaService = new ZerodhaBroker(new UserSessionModel()
                    {
                        AccessToken = userSession.AccessToken,
                        ApiKey = userSession.ApiKey,
                        AppSecret = userSession.AppSecret,
                        PublicToken = userSession.PublicToken,
                        UserId = userSession.UserId
                    });
                    var candles = zeropdhaService.GetData(
                                    symbol, mAStrategy._HistoricalDataTimeframe,
                                    DateTime.Now.AddDays(mAStrategy._HistoricalDataInDays),
                                    //DateTime.Now
                                    new DateTime(2018, 3, 26, 18, 0, 0)
                                    , false);

                    positionTestModels.Add(GetProfitOfDay(symbol, candles));
                });
            }
            Assert.IsTrue(positionTestModels.Where(x => x != null).Sum(x => x.Profit) > 1, "Symbol is Not Profitable");
        }

        [TestMethod]
        public async Task WeeklyWatchlistIsProfitableOrNot()
        {
            List<PositionTestModel> positionWeekTestModels = new List<PositionTestModel>();
            //Get All Candles according to
            MAStrategy mAStrategy = new MAStrategy();
            var watchlistService = new WatchlistService();
            var userSessionService = new UserSessionService();

            var symbols = await watchlistService.GetSymbols(1);
            var userSession = await userSessionService.GetCurrentSession();
            var weekStart = 5;

            if (userSession != null)
            {
                var zeropdhaService = new ZerodhaBroker(new UserSessionModel()
                {
                    AccessToken = userSession.AccessToken,
                    ApiKey = userSession.ApiKey,
                    AppSecret = userSession.AppSecret,
                    PublicToken = userSession.PublicToken,
                    UserId = userSession.UserId
                });

                for (int i = 0; i < 5; i++)
                {
                    List<PositionTestModel> positionTestModels = new List<PositionTestModel>();
                    Parallel.ForEach(symbols.Take(50), symbol =>
                    {
                        var candles = zeropdhaService.GetData(
                                        symbol, mAStrategy._HistoricalDataTimeframe,
                                        new DateTime(2018, 3, weekStart + i, 18, 0, 0).AddDays(mAStrategy._HistoricalDataInDays),
                                        //DateTime.Now
                                        new DateTime(2018, 3, weekStart + i, 18, 0, 0),
                                        false);

                        positionTestModels.Add(GetProfitOfDay(symbol, candles));
                    });

                    positionWeekTestModels.Add(new PositionTestModel()
                    {
                        dateTime = new DateTime(2018, 3, weekStart + i, 0, 0, 0),
                        NoOfPositions = positionTestModels.Where(x => x != null).Sum(x => x.NoOfPositions),
                        NoOfProfitable = positionTestModels.Where(x => x != null).Sum(x => x.NoOfProfitable),
                        NoOfStopHit = positionTestModels.Where(x => x != null).Sum(x => x.NoOfStopHit),
                        Profit = positionTestModels.Where(x => x != null).Sum(x => x.Profit)
                    });
                }
            }
            Assert.IsTrue(positionWeekTestModels.Where(x => x != null).Sum(x => x.Profit) > 1, "Week is Not Profitable");
        }


        [TestMethod]
        public async Task RegressionMonthlyWatchlistIsProfitableOrNot()
        {
            List<PositionTestModel> positionWeekTestModels = new List<PositionTestModel>();
            //Get All Candles according to
            MAStrategy mAStrategy = new MAStrategy();
            var watchlistService = new WatchlistService();
            var userSessionService = new UserSessionService();

            var symbols = await watchlistService.GetSymbols(1);
            var userSession = await userSessionService.GetCurrentSession();
            var weekStart = new DateTime(2018, 2, 5, 9, 0, 0).StartOfWeek(DayOfWeek.Monday);

            IList<PositionTestModel> positionTestModels = new List<PositionTestModel>();

            for (int emaShortPeriodCounter = 3; emaShortPeriodCounter < 20; emaShortPeriodCounter++)
            {
                for (int emaLongPeriodCounter = emaShortPeriodCounter += 1; emaLongPeriodCounter < 25; emaLongPeriodCounter++)
                {
                    mAStrategy._EmaShortPeriod = emaShortPeriodCounter;
                    mAStrategy._EmaLongPeriod = emaLongPeriodCounter;
                    for (int weekCounter = 0; weekCounter < 4; weekCounter++)
                    {
                        DateTime tempWeekStart = weekStart.AddDays(weekCounter * 7);
                        if (userSession != null)
                        {
                            var zeropdhaService = new ZerodhaBroker(new UserSessionModel()
                            {
                                AccessToken = userSession.AccessToken,
                                ApiKey = userSession.ApiKey,
                                AppSecret = userSession.AppSecret,
                                PublicToken = userSession.PublicToken,
                                UserId = userSession.UserId
                            });

                            for (int dayCounter = 0; dayCounter < 5; dayCounter++)
                            {
                                Parallel.ForEach(symbols.Take(50), symbol =>
                                {
                                    var candles = zeropdhaService.GetData(
                                                    symbol, mAStrategy._HistoricalDataTimeframe,
                                                    new DateTime(2018, 3, tempWeekStart.Day + dayCounter, 9, 0, 0).AddDays(mAStrategy._HistoricalDataInDays),
                                                    //DateTime.Now
                                                    new DateTime(2018, 3, tempWeekStart.Day + dayCounter, 18, 0, 0),
                                                    false);

                                    positionTestModels.Add(GetProfitOfDay(symbol, candles, mAStrategy));
                                });

                                positionWeekTestModels.Add(new PositionTestModel()
                                {
                                    EMA = emaShortPeriodCounter + ", " + emaLongPeriodCounter,
                                    Period = mAStrategy._HistoricalDataTimeframe,
                                    dateTime = new DateTime(2018, 3, tempWeekStart.Day + dayCounter, 0, 0, 0),
                                    NoOfPositions = positionTestModels.Where(x => x != null).Sum(x => x.NoOfPositions),
                                    NoOfProfitable = positionTestModels.Where(x => x != null).Sum(x => x.NoOfProfitable),
                                    NoOfStopHit = positionTestModels.Where(x => x != null).Sum(x => x.NoOfStopHit),
                                    Profit = positionTestModels.Where(x => x != null).Sum(x => x.Profit)
                                });
                            }
                        }
                    }
                }
            }

            positionWeekTestModels.WriteCSV(@"D:\Work\Autotrading\Results\RegressionMonth-" + mAStrategy._HistoricalDataTimeframe + ".csv");
            Assert.IsTrue(positionWeekTestModels.Where(x => x != null).Sum(x => x.Profit) > 1, "Week is Not Profitable");
        }

        private PositionTestModel GetProfitOfDay(Symbol symbol, IEnumerable<Candle> candles, MAStrategy mAStrategy = null)
        {
            PositionTestModel positionTestModel = null;
            if (mAStrategy == null)
                mAStrategy = new MAStrategy();
            BrokerOrderModel brokerOrder = null;
            if (candles != null && symbol != null)
            {
                var lastDay = candles.LastOrDefault().DateTime.Day;
                for (int candleIndex = 0; candleIndex < candles.Count(); candleIndex++)
                {
                    var currentCandle = candles.ElementAt(candleIndex);
                    if (currentCandle.DateTime.Day == lastDay && currentCandle.DateTime.TimeOfDay > mAStrategy.marketStart && currentCandle.DateTime.TimeOfDay < mAStrategy.marketEnd)
                    {
                        if (brokerOrder == null)
                        {
                            var scanResult = mAStrategy.Scan(symbol, candles.Take(candleIndex), false);
                            if (scanResult != null)
                            {
                                brokerOrder = scanResult;
                                if (positionTestModel == null)
                                    positionTestModel = new PositionTestModel() { NoOfPositions = 1, TradingSymbol = symbol.TradingSymbol };
                                else
                                    positionTestModel.NoOfPositions += 1;
                            }
                        }
                        else if (brokerOrder.TransactionType == Constants.TRANSACTION_TYPE_BUY)
                        {
                            //Stop Loss Hit
                            if (currentCandle.Low <= brokerOrder.TriggerPrice.Value)
                            {
                                positionTestModel.Profit -= (brokerOrder.StoplossValue.Value * brokerOrder.Quantity);
                                positionTestModel.NoOfStopHit += 1;
                                brokerOrder = null;
                            }
                            else if (currentCandle.High >= (brokerOrder.Price.Value + brokerOrder.SquareOffValue.Value))
                            {
                                positionTestModel.Profit += (brokerOrder.SquareOffValue.Value * brokerOrder.Quantity);
                                positionTestModel.NoOfProfitable += 1;
                                brokerOrder = null;
                            }
                        }
                        else if (brokerOrder.TransactionType == Constants.TRANSACTION_TYPE_SELL)
                        {
                            //Stop Loss Hit
                            if (currentCandle.High >= brokerOrder.TriggerPrice.Value)
                            {
                                positionTestModel.Profit -= (brokerOrder.StoplossValue.Value * brokerOrder.Quantity);
                                positionTestModel.NoOfStopHit += 1;
                                brokerOrder = null;
                            }
                            else if (currentCandle.Low <= (brokerOrder.Price.Value - brokerOrder.SquareOffValue.Value))
                            {
                                positionTestModel.Profit += (brokerOrder.SquareOffValue.Value * brokerOrder.Quantity);
                                positionTestModel.NoOfProfitable += 1;
                                brokerOrder = null;
                            }
                        }
                    }
                }
            }
            return positionTestModel;
        }
    }
}
