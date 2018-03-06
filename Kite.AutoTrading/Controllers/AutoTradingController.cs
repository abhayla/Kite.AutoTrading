using Hangfire;
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
            //var userSession = (UserSessionModel)Session["userSession"];
            //ZerodhaBroker zerodhaBroker = new ZerodhaBroker(userSession);
            //MyFisrtStrategy.Start();
            //RecurringJob.AddOrUpdate(() => MyFisrtStrategy.Start() , Cron.Minutely);


            //RecurringJob.AddOrUpdate<MyFirstStrategy>(job.HangfireId,x=>x.Start(job.Id) , Cron.Minutely);
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
                WatchlistId = 2
            });

            //Stopwatch sw = new Stopwatch();
            MAStrategy my = new MAStrategy();
            //sw.Start();
            await my.Start(job.Id, true);
            //sw.Stop();
            //ApplicationLogger.LogJob(job.Id, "Job Completed at (Seconds)" + sw.Elapsed.TotalSeconds.ToString());

            //RecurringJob.AddOrUpdate<MAStrategy>(job.HangfireId, x => x.Start(job.Id, false), Cron.MinuteInterval(5));
            return View();
        }
    }
}
