using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.EF;
using KiteConnect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Kite.AutoTrading.Data.DataServices
{
    public class BrokerPositionService
    {
        private readonly KiteAutotradingEntities _context;
        public BrokerPositionService()
        {
            _context = new KiteAutotradingEntities();
        }

        public async Task<bool> SyncPositions(IList<Position> positions)
        {
            if (positions != null && positions.Count() > 0)
            {
                foreach (var position in positions)
                {
                    _context.BrokerPositions.Add(new BrokerPosition()
                    {
                        AveragePrice = position.AveragePrice,
                        CreatedDate=DateTime.Now,
                        DayBuyPrice=position.DayBuyPrice,
                        DayBuyQuantity=position.DayBuyQuantity,
                        DayBuyValue=position.DayBuyQuantity,
                        DaySellPrice=position.DaySellPrice,
                        DaySellQuantity=position.DaySellQuantity,
                        DaySellValue=position.DaySellValue,
                        Exchange=position.Exchange,
                        PNL=position.PNL,
                        Realised=position.Realised,
                        TradingSymbol=position.TradingSymbol,
                        Unrealised=position.Unrealised
                    });
                }
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
