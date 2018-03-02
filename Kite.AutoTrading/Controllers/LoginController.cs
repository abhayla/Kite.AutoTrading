using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Common.ViewModels;
using Kite.AutoTrading.Data.DataServices;
using Kite.AutoTrading.Data.EF;
using KiteConnect;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Kite.AutoTrading.Controllers
{
    public class LoginController : Controller
    {
        KiteConnect.Kite kite;
        private readonly UserSessionService _userSessionService;

        public LoginController()
        {
            _userSessionService = new UserSessionService();
        }

        // GET: Login
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            var userSession = await _userSessionService.GetCurrentSession();
            if (userSession == null)
            {
                var loginModel = new LoginViewmodel()
                {
                    ZerodhaUserId = "PS6365",
                    ApiKey = "17fzkglzv7v07xx1",
                    ApiSecret = "tvr9hcb8mt6gf2joq8jl3kn61dt41l9h"
                };
                return View(loginModel);
            }
            else
            {
                Session["userSession"] = AutoMapper.Mapper.Map<UserSessionModel>(userSession);
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<ActionResult> Index(LoginViewmodel model)
        {
            if (ModelState.IsValid)
            {
                kite = new KiteConnect.Kite(APIKey: model.ApiKey, Debug: true);
                
                User user = kite.GenerateSession(model.RequestToken, model.ApiSecret);
                var userSessionModel = new UserSessionModel()
                {
                    AccessToken = user.AccessToken,
                    ApiKey = model.ApiKey,
                    AppSecret = model.ApiSecret,
                    UserId = model.ZerodhaUserId,
                    PublicToken = user.PublicToken
                };
                await _userSessionService.SetCurrentSession(userSessionModel);
                Session["userSession"] = userSessionModel;
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("Something went wrong", "please contact admin");
            return View(model);
        }
    }
}