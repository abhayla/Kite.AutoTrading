using Kite.AutoTrading.Business.Brokers;
using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Kite.AutoTrading.Controllers
{
    public class SymbolController : Controller
    {
        public SymbolController()
        {
            _symbolService = new SymbolService();
        }
        private readonly SymbolService _symbolService;
        [HttpGet]
        public async Task<ActionResult> Sync()
        {
            var userSessionService = new UserSessionService();
            var userSession = await userSessionService.GetCurrentSession();
            var _zerodhaBroker = new ZerodhaBroker(AutoMapper.Mapper.Map<UserSessionModel>(userSession));
            var instruments = _zerodhaBroker._kite.GetInstruments().Where(x=>x.Exchange=="NSE" || x.Exchange=="BSE").ToList();
            
            await _symbolService.Sync(instruments);
            return View();
        }
    }
}