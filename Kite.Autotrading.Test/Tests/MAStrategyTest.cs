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

namespace Kite.Autotrading.Test
{
    [TestClass]
    public class MAStrategyTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            //Get All Candles according to

        }

        private async Task<double> GetProfit(Symbol symbol ,IEnumerable<Candle> candles)
        {
            double profit = 0;
            var mAStrategy = new MAStrategy();
            PositionTestModel position=null;

            for (int candleIndex = 100;candleIndex < candles.Count();candleIndex++)
            {
                if (position != null)
                {
                    var scanResult = mAStrategy.Scan(symbol, candles.Take(candleIndex));
                }
                else
                {

                }
            }
            return profit;
        }
    }
}
