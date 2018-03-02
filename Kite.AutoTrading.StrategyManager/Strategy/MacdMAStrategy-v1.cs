using Kite.AutoTrading.Business.Brokers;
using Kite.AutoTrading.Common.Enums;
using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.DataServices;
using Kite.AutoTrading.Data.EF;
using KiteConnect;
using System;
using System.Collections.Generic;
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
        private readonly int _MacdBackscanNoOfCandles = 3;
        private readonly int _MinQuantity = 1;
        private readonly double _RiskPercentage = 0.003;
        private readonly double _RewardPercentage = 0.008;
        private readonly double _MinPositiveHist = 0.7;
        private readonly double _MinNegativeHist = -0.;

        #endregion

        private readonly JobService _jobService;
        private readonly WatchlistService _watchlistService;
        private readonly UserSessionService _userSessionService;
        private ZerodhaBroker _zeropdhaService;
        private int _jobId;

        public MacdMAStrategy()
        {
            _jobService = new JobService();
            _watchlistService = new WatchlistService();
            _userSessionService = new UserSessionService();
        }

        public async Task Start(int jobId)
        {
            var job = await _jobService.GetJob(jobId);
            _jobId = jobId;
            var symbols = await _watchlistService.GetSymbols(job.Strategy.WatchlistId);
            var userSession = await _userSessionService.GetCurrentSession();
            _zeropdhaService = new ZerodhaBroker(AutoMapper.Mapper.Map<UserSessionModel>(userSession));
            var positions = _zeropdhaService._kite.GetPositions();
            var orders = _zeropdhaService._kite.GetOrders();

            Parallel.ForEach(symbols, symbol =>
            {
                var candles = _zeropdhaService.GetData(symbol, -2, Constants.INTERVAL_5MINUTE);
                if (candles.Count() > 0)
                {
                    var position = positions.Day.Where(x => x.TradingSymbol == symbol.TradingSymbol).FirstOrDefault();
                    var order = orders.Where(x => x.Tradingsymbol == symbol.TradingSymbol && x.Status == "TRIGGER PENDING").FirstOrDefault();
                    if (position.Quantity != 0)
                        SquareOffOpenPosition(symbol, candles, position, order);
                    else
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

        public async Task Stop(int jobId)
        {

        }

        public bool BearishScan(Symbol symbol, IEnumerable<Candle> candles)
        {
            //Bearish Strategy            
            var fiveEma = candles.Ema(5)[candles.Count() - 1];
            var twentyEma = candles.Ema(20)[candles.Count() - 1];
            var twentySma = candles.Sma(20)[candles.Count() - 1];

            if (fiveEma.Tick.Value < twentyEma.Tick.Value && fiveEma.Tick.Value < twentySma.Tick.Value)
            {
                //EMA Satisfied
                for (var bar = 1; bar <= _MacdBackscanNoOfCandles; bar++)
                {
                    var indexedCandle = new IndexedCandle(candles, candles.Count() - bar);
                    var macdResult = indexedCandle.IsMacdBearishCross(12, 26, 9);
                    if (macdResult)
                    {
                        //if macd is crossover in last 5 candles then place order
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
                            TriggerPrice = candles.LastOrDefault().High + (candles.LastOrDefault().High * Convert.ToDecimal(_RiskPercentage))
                        });
                        return true;
                    }
                }
            }
            return false;
        }

        public bool BullishScan(Symbol symbol, IEnumerable<Candle> candles)
        {
            //Bearish Strategy            
            var fiveEma = candles.Ema(5)[candles.Count() - 1];
            var twentyEma = candles.Ema(20)[candles.Count() - 1];
            var twentySma = candles.Sma(20)[candles.Count() - 1];

            if (fiveEma.Tick.Value > twentyEma.Tick.Value && fiveEma.Tick.Value > twentySma.Tick.Value)
            {
                //EMA Satisfied
                for (var bar = 1; bar <= _MacdBackscanNoOfCandles; bar++)
                {
                    var indexedCandle = new IndexedCandle(candles, candles.Count() - bar);
                    var macdResult = indexedCandle.IsMacdBullishCross(12, 26, 9);
                    if (macdResult)
                    {
                        //if macd is crossover in last 5 candles then place order
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
                            TriggerPrice = candles.LastOrDefault().Low + (candles.LastOrDefault().Low * Convert.ToDecimal(_RiskPercentage))
                        });
                        return true;
                    }
                }
            }
            return false;
        }

        public bool SquareOffOpenPosition(Symbol symbol, IEnumerable<Candle> candles, Position position, Order order)
        {
            var indexedCandle = new IndexedCandle(candles, candles.Count() - 1);
            bool macdResult = false;
            if (position.Quantity < 0)
                macdResult = indexedCandle.IsMacdBullishCross(12, 26, 9);
            else
                macdResult = indexedCandle.IsMacdBearishCross(12, 26, 9);

            if (macdResult)
            {
                _zeropdhaService._kite.CancelOrder(order.OrderId, Constants.VARIETY_CO, order.ParentOrderId);
                return true;
            }
            return false;
        }
    }
}
