using Kite.AutoTrading.Business.Brokers;
using Kite.AutoTrading.Common.Enums;
using Kite.AutoTrading.Common.Helper;
using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.DataServices;
using Kite.AutoTrading.Data.EF;
using KiteConnect;
using System;
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
    public class MacdMAStrategy : IStrategy
    {

        #region Configurations

        private readonly int _MinQuantity = 1;
        private readonly int _MaxActivePositions = 10;
        private readonly int _HistoricalDataInDays = -45;
        private readonly string _HistoricalDataTimeframe = Constants.INTERVAL_5MINUTE;
        private readonly decimal _RiskPercentage = 0.003m;
        private readonly decimal _RewardPercentage = 0.008m;
        private readonly int _MacdCrossBacktestStart = 2;
        private readonly int _MacdCrossBacktestEnd = 4;

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

        public async Task Start(int jobId,bool isDevelopment = false)
        {
            StringBuilder sb = new StringBuilder();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ApplicationLogger.LogJob(jobId, " job Started " + DateTime.Now.ToString());
            //check indian standard time
            TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var indianTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, INDIAN_ZONE).TimeOfDay;
            var start = new TimeSpan(9, 30, 0); //10 o'clock
            var end = new TimeSpan(15, 10, 0); //12 o'clock
            if (((indianTime > start) && (indianTime < end)) || isDevelopment)
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
                        var order = orders.Where(x => x.Tradingsymbol == symbol.TradingSymbol && x.Status == "TRIGGER PENDING").OrderByDescending(x=>x.OrderTimestamp).FirstOrDefault();
                        var parentOrder = orders.Where(x => x.OrderId == order.ParentOrderId).FirstOrDefault();
                        if (position.Quantity != 0)
                            SquareOffOpenPosition(symbol, candles, position, order, parentOrder);
                        else if (positions.Day != null && positions.Day.Where(x=>x.Quantity!=0).Count() < _MaxActivePositions)
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
            sw.Stop();
            ApplicationLogger.LogJob(jobId, " job Completed in " + sw.Elapsed.TotalMinutes);
        }

        public async Task Stop(int jobId)
        {

        }

        public bool BullishScan(Symbol symbol, IEnumerable<Candle> candles)
        {
            StringBuilder sb = new StringBuilder();
            var fiveEma = candles.Ema(5)[candles.Count() - 1];
            var twentyEma = candles.Ema(20)[candles.Count() - 1];
            var twentySma = candles.Sma(20)[candles.Count() - 1];
            var currentCandle = new IndexedCandle(candles, candles.Count() - 1);
            var closes = new List<decimal> {23,23};
            var smaTs = closes.MacdHist(12, 26, 9);

            sb.Append(DateTime.Now.ToString() + " - Bullish Scan : " + symbol.TradingSymbol);

            if (fiveEma.Tick.Value > twentyEma.Tick.Value && fiveEma.Tick.Value > twentySma.Tick.Value && currentCandle.IsMacdOscBullish(12, 26, 9))
            {
                sb.Append(" -> EMA Satisfied");
                //Verify MACD from last 2 to 4 candles
                for (int i = _MacdCrossBacktestStart; i <= _MacdCrossBacktestEnd; i++)
                {
                    sb.Append(" -> _MacdCrossBacktestStart - " + i.ToString());
                    var indexedCandleLastN = new IndexedCandle(candles, candles.Count() - i);
                    if (indexedCandleLastN.IsMacdBullishCross(12, 26, 9) && IsHistogramTrending(candles, i))
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
                        sb.Append(" -> Placed Buy Order");
                        ApplicationLogger.LogJob(_jobId, sb.ToString());
                        return true;
                    }
                }
            }
            ApplicationLogger.LogJob(_jobId, sb.ToString());
            return false;
        }

        public bool BearishScan(Symbol symbol, IEnumerable<Candle> candles)
        {
            StringBuilder sb = new StringBuilder();
            var fiveEma = candles.Ema(5)[candles.Count() - 1];
            var twentyEma = candles.Ema(20)[candles.Count() - 1];
            var twentySma = candles.Sma(20)[candles.Count() - 1];
            var currentCandle = new IndexedCandle(candles, candles.Count() - 1);

            sb.Append(DateTime.Now.ToString() + " - Bearish Scan : " + symbol.TradingSymbol);
            if (fiveEma.Tick.Value < twentyEma.Tick.Value && fiveEma.Tick.Value < twentySma.Tick.Value && currentCandle.IsMacdOscBearish(12, 26, 9))
            {
                sb.Append(" -> EMA Satisfied");
                //Verify MACD from last 2 to 4 candles
                for (int i = _MacdCrossBacktestStart; i <= _MacdCrossBacktestEnd; i++)
                {
                    sb.Append(" -> _MacdCrossBacktestStart - " + i.ToString());
                    var indexedCandleLastN = new IndexedCandle(candles, candles.Count() - i);
                    if (indexedCandleLastN.IsMacdBearishCross(12, 26, 9) && IsHistogramTrending(candles, i))
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
                        sb.Append(" -> Placed Sell Order");
                        ApplicationLogger.LogJob(_jobId, sb.ToString());
                        return true;
                    }
                }
            }
            ApplicationLogger.LogJob(_jobId, sb.ToString());
            return false;
        }

        public bool SquareOffOpenPosition(Symbol symbol, IEnumerable<Candle> candles, Position position, Order order, Order parentOrder)
        {
            StringBuilder sb = new StringBuilder();
            var indexedCandle = new IndexedCandle(candles, candles.Count() - 1);
            var rewardAmount = parentOrder.Price * _RewardPercentage;
            sb.Append(DateTime.Now.ToString() + " - SquareOffOpenPosition : " + symbol.TradingSymbol);
            sb.Append(DateTime.Now.ToString() + " - close sell position -> rewardAmount (" + rewardAmount + ") position.Quantity (" + position.Quantity + ") indexedCandle.Close ("+ indexedCandle.Close + ") parentOrder.Price (" + parentOrder.Price + ") indexedCandle.Close <= parentOrder.Price - (rewardAmount) (" + (indexedCandle.Close <= parentOrder.Price - (rewardAmount)) + ") indexedCandle.IsMacdOscBearish(12, 26, 9) == false" + "(" + (indexedCandle.IsMacdOscBearish(12, 26, 9) == false) + ")");
            ApplicationLogger.LogJob(_jobId, sb.ToString());
            if (position.Quantity < 0)
            {
                //close sell position
                if (indexedCandle.Close <= parentOrder.Price - (rewardAmount))
                {
                    _zeropdhaService._kite.CancelOrder(order.OrderId, Constants.VARIETY_CO, order.ParentOrderId);
                    return true;
                }
            }
            else
            {
                //close buy position
                if (indexedCandle.Close >= parentOrder.Price + (rewardAmount))
                {
                    _zeropdhaService._kite.CancelOrder(order.OrderId, Constants.VARIETY_CO, order.ParentOrderId);
                    return true;
                }
            }
            return false;
        }

        private bool IsHistogramTrending(IEnumerable<Candle> candles, int noOfCandles)
        {
            //verify MACD Hist is double the previous candle
            var macdHist = new MovingAverageConvergenceDivergenceHistogram(candles, 12, 26, 9);
            var macdHistLastN = macdHist.Compute(candles.Count() - noOfCandles);
            for (int i = 0; i < noOfCandles - 1; i++)
            {
                if (i == 0)
                {
                    //check that next Hist is 4 times of previous
                    if (Math.Abs(macdHistLastN[i].Tick.Value) < 0 || Math.Abs(macdHistLastN[i + 1].Tick.Value) <= (Math.Abs(macdHistLastN[i].Tick.Value) * 4))
                        return false;
                }
                else
                {
                    //check that next Hist is 1.5 times of previous
                    if (Math.Abs(macdHistLastN[i + 1].Tick.Value) < (Math.Abs(macdHistLastN[i].Tick.Value) * 1.5m))
                        return false;
                }
            }
            return true;
        }
    }
}
