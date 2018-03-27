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

        //public readonly int _MinQuantity = 1;
        public readonly int _MinInvestmentPerOrder = 10000;
        public readonly int _MaxActivePositions = 15;
        public readonly int _HistoricalDataInDays = -10;
        public readonly string _HistoricalDataTimeframe = Constants.INTERVAL_10MINUTE;
        public readonly decimal _RiskPercentage = 0.003m;
        public readonly decimal _RewardPercentage = 0.006m;
        public readonly decimal _BuySellOnRisePercentage = 0.002m;
        public readonly int _OrderExpireTimeMinutes = 10;

        public int _EmaShortPeriod = 3;
        public int _EmaLongPeriod = 5;

        public readonly TimeSpan preMarketStart = new TimeSpan(8, 00, 0);
        public readonly TimeSpan preMarketEnd = new TimeSpan(9, 20, 0);

        public readonly TimeSpan marketStart = new TimeSpan(9, 30, 0);
        public readonly TimeSpan marketEnd = new TimeSpan(15, 05, 0);

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

            if (((indianTime.TimeOfDay > preMarketStart) && (indianTime.TimeOfDay < preMarketEnd)) || isDevelopment)
            {
                //Load History Data
                await CacheHistoryData(indianTime, isDevelopment);
            }

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

                    if (positions.Day != null && positions.Day.Where(x => x.Quantity != 0).Count() < _MaxActivePositions)
                    {
                        await symbols.ParallelForEachAsync(async symbol =>
                        {
                            var candles = await _zeropdhaService.GetCachedDataAsync(symbol, _HistoricalDataTimeframe, indianTime.AddDays(_HistoricalDataInDays),
                                    indianTime,
                                    isDevelopment: isDevelopment);
                            if (candles != null && candles.Count() > 0)
                            {
                                var position = positions.Day.Where(x => x.TradingSymbol == symbol.TradingSymbol).FirstOrDefault();
                                var order = orders.Where(x => x.Tradingsymbol == symbol.TradingSymbol && x.Status == "OPEN");
                                //check whether orde is expired 
                                IsOrderExpired(position, order);
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
                    ApplicationLogger.LogJob(jobId, " Not Authenticated !");
            }
            sw.Stop();
            ApplicationLogger.LogJob(jobId, " job Completed in - " + sw.Elapsed.TotalSeconds + " (Seconds)");
        }

        public BrokerOrderModel Scan(Symbol symbol, IEnumerable<Candle> candles, bool isPlaceOrder = true)
        {
            BrokerOrderModel brokerOrderModel = null;
            try
            {
                var currentCandle = new IndexedCandle(candles, candles.Count() - 1);
                var rsi = candles.Rsi(9)[candles.Count() - 1];
                var buySellRiseValue = currentCandle.Close * Convert.ToDecimal(_BuySellOnRisePercentage);
                var riskValue = currentCandle.Close * Convert.ToDecimal(_RiskPercentage);
                var rewardValue = (currentCandle.Close * Convert.ToDecimal(_RewardPercentage));

                if (currentCandle.Prev.IsEmaBullishCross(_EmaShortPeriod, _EmaLongPeriod)
                    && currentCandle.IsBullishExt() && currentCandle.Prev.IsBullishExt()
                    //&& currentCandle.Prev.Close <= currentCandle.Open
                    //&& currentCandle.Prev.GetBody() < currentCandle.GetBody()
                    && rsi.Tick > 50)
                {
                    brokerOrderModel = new BrokerOrderModel()
                    {
                        JobId = _jobId,
                        SymbolId = symbol.Id,
                        TickSize = symbol.TickSize,
                        Exchange = symbol.Exchange,
                        TradingSymbol = symbol.TradingSymbol,
                        TransactionType = Constants.TRANSACTION_TYPE_BUY,
                        Quantity = Convert.ToInt32(_MinInvestmentPerOrder / currentCandle.Close) > 0 ? Convert.ToInt32(_MinInvestmentPerOrder / currentCandle.Close) : 1,
                        //Price = currentCandle.Close - buySellRiseValue,
                        Price = currentCandle.Close,
                        Product = Constants.PRODUCT_MIS,
                        OrderType = Constants.ORDER_TYPE_LIMIT,
                        Validity = Constants.VALIDITY_DAY,
                        Variety = Constants.VARIETY_BO,
                        //TriggerPrice = (currentCandle.Close - (riskValue + buySellRiseValue)),
                        TriggerPrice = currentCandle.Close - (riskValue),
                        SquareOffValue = rewardValue,
                        StoplossValue = riskValue,
                        TrailingStoploss = (riskValue) < 1 ? 1 : (riskValue)
                    };
                    if (isPlaceOrder)
                        _zeropdhaService.PlaceOrder(brokerOrderModel);
                    //Log Candle
                    ApplicationLogger.LogJob(_jobId,
                        GlobalConfigurations.IndianTime + " Buying Current Candles [" + symbol.TradingSymbol + "]" + JsonConvert.SerializeObject(candles.ElementAt(candles.Count() - 1)) + Environment.NewLine +
                        GlobalConfigurations.IndianTime + " Buying Previous Candles [" + symbol.TradingSymbol + "]" + JsonConvert.SerializeObject(candles.ElementAt(candles.Count() - 2)));
                }
                else if (currentCandle.Prev.IsEmaBearishCross(_EmaShortPeriod, _EmaLongPeriod)
                    && currentCandle.IsBearishExt() && currentCandle.Prev.IsBearishExt()
                    //&& currentCandle.Prev.Close >= currentCandle.Open
                    //&& currentCandle.Prev.GetBody() < currentCandle.GetBody()
                    && rsi.Tick < 50)
                {
                    brokerOrderModel = new BrokerOrderModel()
                    {
                        JobId = _jobId,
                        SymbolId = symbol.Id,
                        TickSize = symbol.TickSize,
                        Exchange = symbol.Exchange,
                        TradingSymbol = symbol.TradingSymbol,
                        TransactionType = Constants.TRANSACTION_TYPE_SELL,
                        Quantity = Convert.ToInt32(_MinInvestmentPerOrder / currentCandle.Close) > 0 ? Convert.ToInt32(_MinInvestmentPerOrder / currentCandle.Close) : 1,
                        //Price = currentCandle.Close + buySellRiseValue,
                        Price = currentCandle.Close,
                        Product = Constants.PRODUCT_MIS,
                        OrderType = Constants.ORDER_TYPE_LIMIT,
                        Validity = Constants.VALIDITY_DAY,
                        Variety = Constants.VARIETY_BO,
                        //TriggerPrice = (currentCandle.Close + (riskValue + buySellRiseValue)),
                        TriggerPrice = (currentCandle.Close + (riskValue)),
                        SquareOffValue = rewardValue,
                        StoplossValue = riskValue,
                        TrailingStoploss = (riskValue) < 1 ? 1 : (riskValue)
                    };
                    if (isPlaceOrder)
                        _zeropdhaService.PlaceOrder(brokerOrderModel);
                    //Log Candle
                    ApplicationLogger.LogJob(_jobId,
                        GlobalConfigurations.IndianTime + " Selling Current Candles [" + symbol.TradingSymbol + "]" + JsonConvert.SerializeObject(candles.ElementAt(candles.Count() - 1)) + Environment.NewLine +
                        GlobalConfigurations.IndianTime + " Selling Previous Candles [" + symbol.TradingSymbol + "]" + JsonConvert.SerializeObject(candles.ElementAt(candles.Count() - 2)));
                }
            }
            catch (Exception ex) { }
            return brokerOrderModel;
        }

        public bool IsOrderExpired(Position position, IEnumerable<Order> orders)
        {
            DateTime istTime = GlobalConfigurations.IndianTime;
            if (orders != null && orders.Count() > 0 && position.Quantity == 0)
            {
                var order = orders.FirstOrDefault();
                if (order.ExchangeTimestamp.HasValue && order.ExchangeTimestamp.Value.AddMinutes(_OrderExpireTimeMinutes) < istTime)
                {
                    //Cancel order because its pending from last 10 minutes
                    ApplicationLogger.LogJob(_jobId, istTime + " Order Expired :" + JsonConvert.SerializeObject(order));
                    _zeropdhaService._kite.CancelOrder(OrderId: order.OrderId, ParentOrderId: order.ParentOrderId, Variety: order.Variety);
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> CacheHistoryData(DateTime indianTime, bool isDevelopment)
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
                    var candles = await _zeropdhaService.GetCachedDataAsync(symbol, _HistoricalDataTimeframe,
                        indianTime.AddDays(_HistoricalDataInDays),
                        indianTime,
                        isDevelopment: isDevelopment);
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
