using Kite.AutoTrading.Data.EF;
using KiteConnect;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Kite.AutoTrading.Data.DataServices
{
    public class StrategyService
    {
        private readonly KiteAutotradingEntities _context;
        public StrategyService()
        {
            _context = new KiteAutotradingEntities();
        }

        public async Task<Strategy> Get(int stratergyId)
        {
            return await _context.Strategies.Where(x => x.Id == stratergyId).FirstOrDefaultAsync();
        }

    }
}
