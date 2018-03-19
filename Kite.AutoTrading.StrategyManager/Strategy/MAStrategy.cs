using Hangfire;
using Kite.AutoTrading.Business.Brokers;
using Kite.AutoTrading.Common.Configurations;
using Kite.AutoTrading.Common.Enums;
using Kite.AutoTrading.Common.Helper;
using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.DataServices;
using Kite.AutoTrading.Data.EF;
using KiteConnect;
using Newtonsoft.Json;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trady.Analysis;
using Trady.Core;
using Kite.AutoTrading.StrategyManager.Helper;


namespace Kite.AutoTrading.StrategyManager.Strategy
{
    public class MAStrategy
    {

        #region Configurations

        private readonly int _MinQuantity = 1;
        private readonly int _MaxActivePositions = 10;
        private readonly int _HistoricalDataInDays = -5;
        private readonly string _HistoricalDataTimeframe = Constants.INTERVAL_3MINUTE;
        private readonly decimal _RiskPercentage = 0.002m;
        private readonly decimal _RewardPercentage = 0.0045m;
        private readonly decimal _BuySellOnRisePercentage = 0.001m;
        private readonly int _OrderExpireTimeMinutes = 10;

        #endregion

        private readonly JobService _jobService;
        private readonly WatchlistService _watchlistService;
        private readonly UserSessionService _userSessionService;
        private readonly StrategyService _strategyService;
        private ZerodhaBroker _zeropdhaService;
        private int _jobId;

        public MAStrategy()
        {
            _jobService = new JobService();
            _watchlistService = new WatchlistService();
            _userSessionService = new UserSessionService();
            _strategyService = new StrategyService();
        }

        [DisableConcurrentExecution(timeoutInSeconds: 3 * 60)]
        public async Task Start(int jobId, bool isDevelopment = false)
        {
            _jobId = jobId;
            StringBuilder sb = new StringBuilder();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var indianTime = GlobalConfigurations.IndianTime;
            ApplicationLogger.LogJob(jobId, " job Started " + indianTime.ToString());

            //check indian standard time
            var marketStart = new TimeSpan(9, 30, 0);
            var marketEnd = new TimeSpan(15, 15, 0);

            var preMarketStart = new TimeSpan(8, 00, 0);
            var preMarketEnd = new TimeSpan(9, 10, 0);

            //if (((indianTime.TimeOfDay > preMarketStart) && (indianTime.TimeOfDay < preMarketEnd)) || isDevelopment)
            //{
            //    //Load History Data
            //    await CacheHistoryData(indianTime.StartOfDay());
            //}

            if (((indianTime.TimeOfDay > marketStart) && (indianTime.TimeOfDay < marketEnd)) || isDevelopment)
            {
                var job = await _jobService.GetJob(jobId);
                var strategy = await _strategyService.Get(job.StrategyId);
                var symbols = await _watchlistService.GetSymbols(strategy.WatchlistId);

                var userSession = await _userSessionService.GetCurrentSession();
                if (userSession != null)
                {
                    _zeropdhaService = new ZerodhaBroker(AutoMapper.Mapper.Map<UserSessionModel>(userSession));
                    var positions = _zeropdhaService._kite.GetPositions();
                    var orders = _zeropdhaService._kite.GetOrders();
                    if (positions.Day.Count() < _MaxActivePositions)
                    {
                        await symbols.ParallelForEachAsync(async symbol =>
                        {
                            var candles = await _zeropdhaService.GetCachedDataAsync(symbol, _HistoricalDataTimeframe, indianTime.AddDays(_HistoricalDataInDays),
                                    indianTime, isDevelopment: isDevelopment);
                            if (candles != null && candles.Count() > 0)
                            {
                                var position = positions.Day.Where(x => x.TradingSymbol == symbol.TradingSymbol).FirstOrDefault();
                                var order = orders.Where(x => x.Tradingsymbol == symbol.TradingSymbol && x.Status == "OPEN");
                                //check whether orde is expired 
                                IsOrderExpired(position, order, indianTime);
                                if (position.Quantity == 0 && order.Count() == 0)
                                    Scan(symbol, candles);
                            }
                        });
                    }

                    //Update Status after every round of scanning
                    job.Status = JobStatus.Running.ToString();
                    job.ModifiedDate = DateTime.Now;
                    await _jobService.Update(job);
                }
                else
                    ApplicationLogger.LogJob(jobId, " Not Authenticated ! ");
            }
            sw.Stop();
            ApplicationLogger.LogJob(jobId, " job Completed in (Minutes) - " + sw.Elapsed.TotalMinutes);
        }

        public async Task Stop(int jobId)
        {

        }

        public bool Scan(Symbol symbol, IEnumerable<Candle> candles)
        {
            var currentCandle = new IndexedCandle(candles, candles.Count() - 1);

            var riskValue = currentCandle.Close * Convert.ToDecimal(_RiskPercentage);
            var rewardValue = (currentCandle.Close * Convert.ToDecimal(_RewardPercentage));
            var buySellRiseValue = currentCandle.Close * Convert.ToDecimal(_BuySellOnRisePercentage);

            if (currentCandle.Prev.IsEmaBullishCross(5, 20) //EMA must be Bullish
                && currentCandle.IsBullishExt() && currentCandle.Prev.IsBullishExt()  //Both candles must be Bullish or Green candles
                && currentCandle.Prev.Close <= currentCandle.Open
                && currentCandle.Prev.GetBody() < currentCandle.GetBody())
            {
                //Place Buy Order
                _zeropdhaService.PlaceOrder(new BrokerOrderModel()
                {
                    JobId = _jobId,
                    SymbolId = symbol.Id,
                    TickSize = symbol.TickSize,
                    Exchange = symbol.Exchange,
                    TradingSymbol = symbol.TradingSymbol,
                    TransactionType = Constants.TRANSACTION_TYPE_BUY,
                    Quantity = _MinQuantity,
                    Price = currentCandle.Close - buySellRiseValue,
                    Product = Constants.PRODUCT_MIS,
                    OrderType = Constants.ORDER_TYPE_LIMIT,
                    Validity = Constants.VALIDITY_DAY,
                    Variety = Constants.VARIETY_BO,
                    TriggerPrice = (currentCandle.Close - riskValue),
                    SquareOffValue = rewardValue,
                    StoplossValue = riskValue,
                    TrailingStoploss = (riskValue * 0.75m) < 1 ? 1 : (riskValue * 0.75m)
                });
                //Log Candle
                ApplicationLogger.LogJob(_jobId,
                    GlobalConfigurations.IndianTime + " Buying Current Candles [" + symbol.TradingSymbol + "]" + JsonConvert.SerializeObject(candles.ElementAt(candles.Count() - 1)) + Environment.NewLine +
                    GlobalConfigurations.IndianTime + " Buying Previous Candles [" + symbol.TradingSymbol + "]" + JsonConvert.SerializeObject(candles.ElementAt(candles.Count() - 2)));
                return true;
            }
            else if (currentCandle.Prev.IsEmaBearishCross(5, 20)
                && currentCandle.IsBearishExt() && currentCandle.Prev.IsBearishExt()
                && currentCandle.Prev.Close >= currentCandle.Open
                && currentCandle.Prev.GetBody() < currentCandle.GetBody())
            {
                //Place SELL Order
                _zeropdhaService.PlaceOrder(new BrokerOrderModel()
                {
                    JobId = _jobId,
                    SymbolId = symbol.Id,
                    TickSize = symbol.TickSize,
                    Exchange = symbol.Exchange,
                    TradingSymbol = symbol.TradingSymbol,
                    TransactionType = Constants.TRANSACTION_TYPE_SELL,
                    Quantity = _MinQuantity,
                    Price = currentCandle.Close + buySellRiseValue,
                    Product = Constants.PRODUCT_MIS,
                    OrderType = Constants.ORDER_TYPE_LIMIT,
                    Validity = Constants.VALIDITY_DAY,
                    Variety = Constants.VARIETY_BO,
                    TriggerPrice = (currentCandle.Close + riskValue),
                    SquareOffValue = rewardValue,
                    StoplossValue = riskValue,
                    TrailingStoploss = (riskValue * 0.75m) < 1 ? 1 : (riskValue * 0.75m)
                });
                //Log Candle
                ApplicationLogger.LogJob(_jobId,
                    GlobalConfigurations.IndianTime + " Selling Current Candles [" + symbol.TradingSymbol + "]" + JsonConvert.SerializeObject(candles.ElementAt(candles.Count() - 1)) + Environment.NewLine +
                    GlobalConfigurations.IndianTime + " Selling Previous Candles [" + symbol.TradingSymbol + "]" + JsonConvert.SerializeObject(candles.ElementAt(candles.Count() - 2)));
                return true;
            }
            return false;
        }

        public bool IsOrderExpired(Position position, IEnumerable<Order> orders, DateTime istTime)
        {
            if (orders != null && orders.Count() > 0 && position.Quantity == 0)
            {
                var order = orders.FirstOrDefault();
                if (order.OrderTimestamp.HasValue && order.OrderTimestamp.Value.AddMinutes(_OrderExpireTimeMinutes) > istTime)
                {
                    //Cancel order because its pending from last 10 minutes
                    ApplicationLogger.LogJob(_jobId, istTime + " Order Expired :" + JsonConvert.SerializeObject(order));
                    _zeropdhaService._kite.CancelOrder(OrderId: order.OrderId, ParentOrderId: order.ParentOrderId, Variety: order.Variety);
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> CacheHistoryData(DateTime indianTime)
        {
            //Delete Previous data
            //Directory.Delete(GlobalConfigurations.CachedDataPath , recursive: true);

            var job = await _jobService.GetJob(_jobId);
            var strategy = await _strategyService.Get(job.StrategyId);
            var symbols = await _watchlistService.GetSymbols(strategy.WatchlistId);

            var userSession = await _userSessionService.GetCurrentSession();
            if (userSession != null)
            {
                _zeropdhaService = new ZerodhaBroker(AutoMapper.Mapper.Map<UserSessionModel>(userSession));
                await symbols.ParallelForEachAsync(async symbol =>
                {
                    var candles = await _zeropdhaService.GetCachedDataAsync(symbol, _HistoricalDataTimeframe, indianTime.AddDays(_HistoricalDataInDays),
                            indianTime);
                    if (candles != null && candles.Count() > 0)
                        ApplicationLogger.LogJob(_jobId, "Trading Data Cached :" + symbol.TradingSymbol);
                    else
                        ApplicationLogger.LogJob(_jobId, "Trading Data Not Cached :" + symbol.TradingSymbol);

                });
            }
            return true;
        }
    }
}
