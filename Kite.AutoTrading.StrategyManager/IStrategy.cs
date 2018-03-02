using Kite.AutoTrading.Data.EF;
using KiteConnect;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trady.Core;

namespace Kite.AutoTrading.StrategyManager.Strategy
{
    public interface IStrategy
    {
        Task Start(int jobId, bool isDevelopment = false);
        Task Stop(int jobId);
        bool BullishScan(Symbol symbol, IEnumerable<Candle> candles);
        bool BearishScan(Symbol symbol, IEnumerable<Candle> candles);
        bool SquareOffOpenPosition(Symbol symbol, IEnumerable<Candle> candles, Position position, Order order, Order parentOrder);
    }
}