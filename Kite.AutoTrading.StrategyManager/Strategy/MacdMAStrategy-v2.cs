using Kite.AutoTrading.Business.Brokers;
using Kite.AutoTrading.Common.Enums;
using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.DataServices;
using Kite.AutoTrading.Data.EF;
using KiteConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Trady.Analysis;
using Trady.Analysis.Indicator;
using Trady.Core;

namespace Kite.AutoTrading.StrategyManager.Strategy
{
    public class MacdMAStrategy : IStrategy
    {

        #region Configurations

        private readonly int _MinQuantity = 1;
        private readonly int _MaxActivePositions = 10;
        private readonly int _HistoricalDataInDays = -45;
        private readonly string _HistoricalDataTimeframe = Constants.INTERVAL_5MINUTE;
        private readonly decimal _RiskPercentage = 0.003m;
        private readonly decimal _RewardPercentage = 0.008m;
        private readonly decimal _MinPositiveMacdHist = 0.55m;
        private readonly decimal _MaxPositiveMacdHist = 1;
        private readonly decimal _MinNegativeMacdMHist = -0.55m;
        private readonly decimal _MaxNegativeMacdHist = -1;
        
        #endregion

        private readonly JobService _jobService;
        private readonly WatchlistService _watchlistService;
        private readonly UserSessionService _userSessionService;
        private readonly StrategyService _strategyService;
        private ZerodhaBroker _zeropdhaService;
        private int _jobId;

        public MacdMAStrategy()
        {
            _jobService = new JobService();
            _watchlistService = new WatchlistService();
            _userSessionService = new UserSessionService();
            _strategyService = new StrategyService();
        }

        //public async Task Start(int jobId)
        //{
        //    //check indian standard time
        //    TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        //    var indianTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, INDIAN_ZONE).TimeOfDay;
        //    var start = new TimeSpan(10, 0, 0); //10 o'clock
        //    var end = new TimeSpan(18, 30, 0); //12 o'clock
        //    if ((indianTime > start) && (indianTime < end))
        //    {
        //        _jobId = jobId;
        //        var job = await _jobService.GetJob(jobId);
        //        var strategy = await _strategyService.Get(job.StrategyId);
        //        var symbols = await _watchlistService.GetSymbols(strategy.WatchlistId);

        //        var userSession = await _userSessionService.GetCurrentSession();
        //        _zeropdhaService = new ZerodhaBroker(AutoMapper.Mapper.Map<UserSessionModel>(userSession));
        //        var positions = _zeropdhaService._kite.GetPositions();
        //        var orders = _zeropdhaService._kite.GetOrders();

        //        Parallel.ForEach(symbols, symbol =>
        //        {
        //            var candles = _zeropdhaService.GetData(symbol, _HistoricalDataInDays, _HistoricalDataTimeframe);
        //            if (candles != null && candles.Count() > 0)
        //            {
        //                var position = positions.Day.Where(x => x.TradingSymbol == symbol.TradingSymbol).FirstOrDefault();
        //                var order = orders.Where(x => x.Tradingsymbol == symbol.TradingSymbol && x.Status == "TRIGGER PENDING").FirstOrDefault();
        //                var parentOrder = orders.Where(x => x.OrderId == order.ParentOrderId).FirstOrDefault();
        //                if (position.Quantity != 0)
        //                    SquareOffOpenPosition(symbol, candles, position, order, parentOrder);
        //                else if (positions.Day != null && positions.Day.Count < _MaxActivePositions)
        //                {
        //                    if (!BullishScan(symbol, candles))
        //                        BearishScan(symbol, candles);
        //                }
        //            }
        //        });

        //        //Update Status after every round of scanning
        //        job.Status = JobStatus.Running.ToString();
        //        job.ModifiedDate = DateTime.Now;
        //        await _jobService.Update(job);
        //    }
        //}

        public async Task Start(int jobId)
        {
            _jobId = jobId;
            var job = await _jobService.GetJob(jobId);
            var strategy = await _strategyService.Get(job.StrategyId);
            var symbols = await _watchlistService.GetSymbols(strategy.WatchlistId);

            var userSession = await _userSessionService.GetCurrentSession();
            _zeropdhaService = new ZerodhaBroker(AutoMapper.Mapper.Map<UserSessionModel>(userSession));
            var positions = _zeropdhaService._kite.GetPositions();
            var orders = _zeropdhaService._kite.GetOrders();

            Parallel.ForEach(symbols, symbol =>
            {
                var candles = _zeropdhaService.GetData(symbol, _HistoricalDataInDays, _HistoricalDataTimeframe);
                if (candles != null && candles.Count() > 0)
                {
                    var position = positions.Day.Where(x => x.TradingSymbol == symbol.TradingSymbol).FirstOrDefault();
                    var order = orders.Where(x => x.Tradingsymbol == symbol.TradingSymbol && x.Status == "TRIGGER PENDING").FirstOrDefault();
                    var parentOrder = orders.Where(x => x.OrderId == order.ParentOrderId).FirstOrDefault();
                    BullishScan(symbol, candles);
                }
            });

            //Update Status after every round of scanning
            job.Status = JobStatus.Running.ToString();
            job.ModifiedDate = DateTime.Now;
            await _jobService.Update(job);
        }

        public async Task Stop(int jobId)
        {

        }

        public bool BullishScan(Symbol symbol, IEnumerable<Candle> candles)
        {
            var fiveEma = candles.Ema(5)[candles.Count() - 1];
            var twentyEma = candles.Ema(20)[candles.Count() - 1];
            var twentySma = candles.Sma(20)[candles.Count() - 1];
            var macdHist = candles.MacdHist(12, 26, 9)[candles.Count() - 1];
            var indexedCandle = new IndexedCandle(candles, candles.Count() - 8);
            var macdBullOsc = indexedCandle.IsMacdOscBullish(12, 26, 9);
            var macdHist1 = new MovingAverageConvergenceDivergenceHistogram(candles, 12, 26, 9);
            var diff = macdHist1.ComputeNeighbourDiff(candles.Count() - 1);
            var diffPc = macdHist1.ComputeNeighbourPcDiff(candles.Count() - 1);
            var abcd = macdHist1.ComputeDiff(candles.Count() - 4, candles.Count() - 1);

            if (fiveEma.Tick.Value > twentyEma.Tick.Value && fiveEma.Tick.Value > twentySma.Tick.Value && (macdHist.Tick.Value >= _MinPositiveMacdHist && macdHist.Tick.Value <= _MaxPositiveMacdHist) && macdBullOsc)
            {
                var currentCandle = candles.LastOrDefault();
                // 
                //EMA Satisfied
                //_zeropdhaService.PlaceOrder(new BrokerOrderModel()
                //{
                //    JobId = _jobId,
                //    SymbolId = symbol.Id,
                //    Exchange = symbol.Exchange,
                //    TradingSymbol = symbol.TradingSymbol,
                //    InstrumentToken = symbol.InstrumentToken,
                //    TransactionType = Constants.TRANSACTION_TYPE_BUY,
                //    Quantity = _MinQuantity,
                //    OrderType = Constants.ORDER_TYPE_MARKET,
                //    Product = Constants.PRODUCT_MIS,
                //    Variety = Constants.VARIETY_CO,
                //    Validity = Constants.VALIDITY_DAY,
                //    TriggerPrice = Convert.ToInt32(((currentCandle.Close - (currentCandle.Close * Convert.ToDecimal(_RiskPercentage))) / symbol.TickSize)) * symbol.TickSize
                //});
                return true;
            }
            return false;
        }

        public bool BearishScan(Symbol symbol, IEnumerable<Candle> candles)
        {
            //Bearish Strategy            
            var fiveEma = candles.Ema(5)[candles.Count() - 1];
            var twentyEma = candles.Ema(20)[candles.Count() - 1];
            var twentySma = candles.Sma(20)[candles.Count() - 1];
            var macdHist = candles.MacdHist(12, 26, 9)[candles.Count() - 1];
            var indexedCandle = new IndexedCandle(candles, candles.Count() - 1);
            var macdBearOsc = indexedCandle.IsMacdOscBearish(12, 26, 9);

            if (fiveEma.Tick.Value < twentyEma.Tick.Value && fiveEma.Tick.Value < twentySma.Tick.Value && (macdHist.Tick.Value >= _MinNegativeMacdMHist && macdHist.Tick.Value <= _MaxNegativeMacdHist) && macdBearOsc)
            {
                var currentCandle = candles.LastOrDefault();
                //EMA and MACD Satisfied
                _zeropdhaService.PlaceOrder(new BrokerOrderModel()
                {
                    JobId = _jobId,
                    SymbolId = symbol.Id,
                    Exchange = symbol.Exchange,
                    TradingSymbol = symbol.TradingSymbol,
                    InstrumentToken = symbol.InstrumentToken,
                    TransactionType = Constants.TRANSACTION_TYPE_SELL,
                    Quantity = _MinQuantity,
                    OrderType = Constants.ORDER_TYPE_MARKET,
                    Product = Constants.PRODUCT_MIS,
                    Variety = Constants.VARIETY_CO,
                    Validity = Constants.VALIDITY_DAY,
                    TriggerPrice = Convert.ToInt32(((currentCandle.Close + (currentCandle.Close * Convert.ToDecimal(_RiskPercentage))) / symbol.TickSize)) * symbol.TickSize
                });
                return true;
            }
            return false;
        }

        public bool SquareOffOpenPosition(Symbol symbol, IEnumerable<Candle> candles, Position position, Order order, Order parentOrder)
        {
            var indexedCandle = new IndexedCandle(candles, candles.Count() - 1);
            var rewardAmount = parentOrder.Price * _RewardPercentage;
            if (position.Quantity < 0)
            {
                //close sell position
                if (indexedCandle.Close <= parentOrder.Price - (rewardAmount) || indexedCandle.IsMacdOscBearish(12, 26, 9) == false)
                {
                    _zeropdhaService._kite.CancelOrder(order.OrderId, Constants.VARIETY_CO, order.ParentOrderId);
                    return true;
                }
            }
            else
            {
                //close buy position
                if (indexedCandle.Close >= parentOrder.Price + (rewardAmount) || indexedCandle.IsMacdOscBullish(12, 26, 9) == false)
                {
                    _zeropdhaService._kite.CancelOrder(order.OrderId, Constants.VARIETY_CO, order.ParentOrderId);
                    return true;
                }
            }
            return false;
        }
    }
}
