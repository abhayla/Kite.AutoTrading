using Kite.AutoTrading.Common.ViewModels;
using Kite.AutoTrading.Data.EF;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Kite.AutoTrading.Data.DataServices
{
    public class JobService
    {
        private readonly KiteAutotradingEntities _context;
        public JobService()
        {
            _context = new KiteAutotradingEntities();
        }

        public async Task<Job> GetJob(int JobId)
        {
            return await _context.Jobs.Where(x => x.Id == JobId).FirstOrDefaultAsync();
        }

        public async Task<Job> Create(JobViewModel jobViewmodel)
        {
            if (jobViewmodel.WatchlistId > 0)
            {
                var strategy = new Strategy()
                {
                    MaxLoss = jobViewmodel.MaxLoss,
                    MaxProfit = jobViewmodel.MaxProfit,
                    WatchlistId = jobViewmodel.WatchlistId
                };
                _context.Strategies.Add(strategy);
                await _context.SaveChangesAsync();

                var job = new Job()
                {
                    CreatedDate = DateTime.Now,
                    HangfireId = Guid.NewGuid().ToString(),
                    StrategyId = strategy.Id,

                };

                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();
                return job;
            }
            return null;
        }

        public async Task<bool> Update(Job job)
        {
            _context.Entry(job).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}