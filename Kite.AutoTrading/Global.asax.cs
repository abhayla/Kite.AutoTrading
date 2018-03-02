using Kite.AutoTrading.App_Start;
using Kite.AutoTrading.Common.Configurations;
using Kite.AutoTrading.Filters;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Kite.AutoTrading
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AutoMapper.Mapper.Initialize(cfg => cfg.AddProfile<AutoMapperProfile>());
            GlobalFilters.Filters.Add(new GlobalExceptionFilter());

            GlobalConfigurations.LogPath = HttpContext.Current.Server.MapPath("~/Logs/");
        }
    }
}
