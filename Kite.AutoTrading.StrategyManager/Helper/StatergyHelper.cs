using Trady.Analysis;

namespace Kite.AutoTrading.StrategyManager.Helper
{
    public static class StatergyHelper
    {
        public static bool IsBullishExt(this IndexedCandle indexedCandle)
        {
            if (indexedCandle.Open <= indexedCandle.Close)
                return true;
            return false;
        }

        public static bool IsBearishExt(this IndexedCandle indexedCandle)
        {
            if (indexedCandle.Open >= indexedCandle.Close)
                return true;
            return false;
        }
    }
}
