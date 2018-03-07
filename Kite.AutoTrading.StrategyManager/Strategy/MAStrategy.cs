using Kite.AutoTrading.Business.Brokers;
using Kite.AutoTrading.Common.Configurations;
using Kite.AutoTrading.Common.Enums;
using Kite.AutoTrading.Common.Helper;
using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.DataServices;
using Kite.AutoTrading.Data.EF;
using KiteConnect;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trady.Analysis;
using Trady.Analysis.Indicator;
using Trady.Core;

namespace Kite.AutoTrading.StrategyManager.Strategy
{
    public class MAStrategy : IStrategy
    {

        #region Configurations

        private readonly int _MinQuantity = 1;
        private readonly int _MaxActivePositions = 10;
        private readonly int _HistoricalDataInDays = -30;
        private readonly string _HistoricalDataTimeframe = Constants.INTERVAL_10MINUTE;
        private readonly decimal _RiskPercentage = 0.003m;
        private readonly decimal _RewardPercentage = 0.005m;

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

        public async Task Start(int jobId, bool isDevelopment = false)
        {
            StringBuilder sb = new StringBuilder();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            var indianTime = GlobalConfigurations.IndianTime;
            ApplicationLogger.LogJob(jobId, " job Started " + indianTime.ToString());

            //check indian standard time
            var start = new TimeSpan(9, 35, 0); //10 o'clock
            var end = new TimeSpan(15, 15, 0); //12 o'clock
            if (((indianTime.TimeOfDay > start) && (indianTime.TimeOfDay < end)) || isDevelopment)
            {
                _jobId = jobId;
                var job = await _jobService.GetJob(jobId);
                var strategy = await _strategyService.Get(job.StrategyId);
                var symbols = await _watchlistService.GetSymbols(strategy.WatchlistId);

                var userSession = await _userSessionService.GetCurrentSession();
                if (userSession != null)
                {
                    _zeropdhaService = new ZerodhaBroker(AutoMapper.Mapper.Map<UserSessionModel>(userSession));
                    var positions = _zeropdhaService._kite.GetPositions();
                    var orders = _zeropdhaService._kite.GetOrders();

                    await symbols.ParallelForEachAsync(async symbol =>
                    {
                        //var candles = await _zeropdhaService.GetCachedDataAsync(symbol, _HistoricalDataTimeframe, indianTime.AddDays(_HistoricalDataInDays),indianTime);
                        var candles = await _zeropdhaService.GetCachedDataAsync(symbol, _HistoricalDataTimeframe, indianTime.AddDays(_HistoricalDataInDays), 
                            indianTime);
                        if (candles != null && candles.Count() > 0)
                        {
                            var position = positions.Day.Where(x => x.TradingSymbol == symbol.TradingSymbol).FirstOrDefault();
                            var order = orders.Where(x => x.Tradingsymbol == symbol.TradingSymbol && x.Status == "TRIGGER PENDING").OrderByDescending(x => x.OrderTimestamp).FirstOrDefault();
                            var parentOrder = orders.Where(x => x.OrderId == order.ParentOrderId).FirstOrDefault();
                            if (position.Quantity != 0)
                                SquareOffOpenPosition(symbol, candles, position, order, parentOrder);
                            else if (positions.Day != null && positions.Day.Count() < _MaxActivePositions)
                            {
                                if (!BullishScan(symbol, candles))
                                    BearishScan(symbol, candles);
                            }
                        }
                    });

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

        public bool BullishScan(Symbol symbol, IEnumerable<Candle> candles)
        {
            var currentCandle = new IndexedCandle(candles, candles.Count() - 1);
            var crossoverCandle = new IndexedCandle(candles, candles.Count() - 2);

            if (crossoverCandle.IsSmaBullishCross(5, 20) && crossoverCandle.Close <= currentCandle.Open && currentCandle.IsEmaBullish(5))
            {
                _zeropdhaService.PlaceOrder(new BrokerOrderModel()
                {
                    JobId = _jobId,
                    SymbolId = symbol.Id,
                    Exchange = symbol.Exchange,
                    TradingSymbol = symbol.TradingSymbol,
                    InstrumentToken = symbol.InstrumentToken,
                    TransactionType = Constants.TRANSACTION_TYPE_BUY,
                    Quantity = _MinQuantity,
                    OrderType = Constants.ORDER_TYPE_MARKET,
                    Product = Constants.PRODUCT_MIS,
                    Variety = Constants.VARIETY_CO,
                    Validity = Constants.VALIDITY_DAY,
                    TriggerPrice = Convert.ToInt32(((currentCandle.Close - (currentCandle.Close * Convert.ToDecimal(_RiskPercentage))) / symbol.TickSize)) * symbol.TickSize
                });
                return true;
            }
            return false;
        }

        public bool BearishScan(Symbol symbol, IEnumerable<Candle> candles)
        {
            var currentCandle = new IndexedCandle(candles, candles.Count() - 1);
            var crossoverCandle = new IndexedCandle(candles, candles.Count() - 2);

            if (crossoverCandle.IsSmaBearishCross(5, 20) && crossoverCandle.Close >= currentCandle.Open && currentCandle.IsEmaBearish(5))
            {
                _zeropdhaService.PlaceOrder(new BrokerOrderModel()
                {
                    JobId = _jobId,
                    SymbolId = symbol.Id,
                    Exchange = symbol.Exchange,
                    TradingSymbol = symbol.TradingSymbol,
                    InstrumentToken = symbol.InstrumentToken,
                    TransactionType = Constants.TRANSACTION_TYPE_BUY,
                    Quantity = _MinQuantity,
                    OrderType = Constants.ORDER_TYPE_MARKET,
                    Product = Constants.PRODUCT_MIS,
                    Variety = Constants.VARIETY_CO,
                    Validity = Constants.VALIDITY_DAY,
                    TriggerPrice = Convert.ToInt32(((currentCandle.Close - (currentCandle.Close * Convert.ToDecimal(_RiskPercentage))) / symbol.TickSize)) * symbol.TickSize
                });
                return true;
            }
            return false;
        }

        public bool SquareOffOpenPosition(Symbol symbol, IEnumerable<Candle> candles, Position position, Order order, Order parentOrder)
        {
            var indexedCandle = new IndexedCandle(candles, candles.Count() - 1);
            var rewardAmount = parentOrder.AveragePrice * _RewardPercentage;
            if (position.Quantity < 0)
            {           
                //close sell position
                if (indexedCandle.Close <= parentOrder.AveragePrice - (rewardAmount))
                {
                    ApplicationLogger.LogJob(_jobId, " SquareOffOpenPosition  (BUY) -> indexedCandle.Close -> (" + indexedCandle.Close + ")" +
                    " parentOrder.Price - (rewardAmount) -> " + (parentOrder.AveragePrice - (rewardAmount)) +
                    " indexedCandle.IsMacdOscBullish(12,26,9) -> " + indexedCandle.IsMacdOscBullish(12, 26, 9));
                    _zeropdhaService._kite.CancelOrder(order.OrderId, Constants.VARIETY_CO, order.ParentOrderId);
                    return true;
                }
            }
            else
            {
                //close buy position
                if (indexedCandle.Close >= parentOrder.Price + (rewardAmount))
                {
                    ApplicationLogger.LogJob(_jobId, " SquareOffOpenPosition  (BUY) -> indexedCandle.Close -> (" + indexedCandle.Close + ")" +
                    " parentOrder.Price - (rewardAmount) -> " + (parentOrder.AveragePrice - (rewardAmount)) +
                    " indexedCandle.IsMacdOscBullish(12,26,9) -> " + indexedCandle.IsMacdOscBullish(12, 26, 9));
                    _zeropdhaService._kite.CancelOrder(order.OrderId, Constants.VARIETY_CO, order.ParentOrderId);
                    _zeropdhaService._kite.CancelOrder(order.OrderId, Constants.VARIETY_CO, order.ParentOrderId);
                    return true;
                }
            }
            return false;
        }
        
    }
}
