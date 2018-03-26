using Hangfire;
using Kite.AutoTrading.Common.Configurations;
using Kite.AutoTrading.Common.Enums;
using Kite.AutoTrading.Common.Helper;
using Kite.AutoTrading.Common.ViewModels;
using Kite.AutoTrading.Data.DataServices;
using Kite.AutoTrading.StrategyManager.Strategy;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Kite.AutoTrading.Controllers
{
    public class AutoTradingController : BaseController
    {
        private readonly JobService _jobService;

        public AutoTradingController()
        {
            _jobService = new JobService();
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Start()
        {            
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Start(JobViewModel jobViewModel)
        {

            //Create a Job
            var job = await _jobService.Create(new JobViewModel()
            {
                MaxLoss = 10,
                MaxProfit = 10,
                WatchlistId = 1
            });

            //MAStrategy my = new MAStrategy();
            //await my.Start(job.Id, true);
            
            RecurringJob.AddOrUpdate<MAStrategy>(job.HangfireId, x => x.Start(job.Id, false),Cron.MinuteInterval(5));
            return View();
        }
    }
}
