using Kite.AutoTrading.Data.EF;
using KiteConnect;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Kite.AutoTrading.Data.DataServices
{
    public class SymbolService
    {
        private readonly KiteAutotradingEntities _context;
        public SymbolService()
        {
            _context = new KiteAutotradingEntities();
        }

        public async Task<Symbol> Get(int symbolId)
        {
            return await _context.Symbols.Where(x => x.Id == symbolId).FirstOrDefaultAsync();
        }

        public async Task<bool> Sync(IList<Instrument> instruments)
        {
            //await _context.Database.ExecuteSqlCommandAsync("TRUNCATE TABLE trading.Symbol");
            //await _context.SaveChangesAsync();
            //var symbols = _context.Symbols.Select(x=> uint.Parse( x.InstrumentToken)).ToList();
            foreach (var instrument in instruments)
            {
                _context.Symbols.Add(new Symbol()
                {
                    Exchange = instrument.Exchange,
                    ExchangeToken = Convert.ToString(instrument.ExchangeToken),
                    TradingSymbol = instrument.TradingSymbol,
                    TickSize = instrument.TickSize,
                    Expiry = instrument.Expiry,
                    CreatedDate = DateTime.Now,
                    InstrumentToken = Convert.ToString(instrument.InstrumentToken),
                    InstrumentType = instrument.InstrumentType,
                    LotSize = instrument.LotSize,
                    Name = instrument.Name,
                    Segment = instrument.Segment,
                    Strike = instrument.Strike
                });                
            }
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
