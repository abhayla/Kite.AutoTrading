using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Hangfire;

[assembly: OwinStartup(typeof(Kite.AutoTrading.Startup))]

namespace Kite.AutoTrading
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalConfiguration.Configuration
                .UseSqlServerStorage("LocalConnection");

            app.UseHangfireDashboard();
            app.UseHangfireServer();
        }
    }
}
