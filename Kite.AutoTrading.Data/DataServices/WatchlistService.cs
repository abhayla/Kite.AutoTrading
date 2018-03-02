using Kite.AutoTrading.Data.EF;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Kite.AutoTrading.Data.DataServices
{
    public class WatchlistService
    {
        private readonly KiteAutotradingEntities _context;
        public WatchlistService()
        {
            _context = new KiteAutotradingEntities();
        }

        public async Task<IList<Symbol>> GetSymbols(int? watchlistId)
        {
            if (watchlistId.Value > 0)
            {
                var mappings = await _context.WatchlistSymbolMappings.Where(x => x.WatchlistId == watchlistId).Select(x => x.SymbolId).ToListAsync();

                return _context.Symbols.Where(x => mappings.Contains(x.Id)).ToList();
            }
            return null;
        }
    }
}
