using System;

namespace Kite.Autotrading.Test
{
    public class PositionTestModel
    {
        public string TradingSymbol { get; set; }
        public int NoOfPositions { get; set; }
        public int NoOfProfitable { get; set; }
        public int NoOfStopHit { get; set; }
        public decimal Profit { get; set; }
        public DateTime dateTime { get; set; }
    }
}
