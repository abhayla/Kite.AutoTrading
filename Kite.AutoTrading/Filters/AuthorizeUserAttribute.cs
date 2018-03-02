using Kite.AutoTrading.Common.Models;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Kite.AutoTrading.Filters
{
    public class AuthorizeUserAttribute : AuthorizeAttribute
    {
        // Custom property
        public string AccessLevel { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var userSession = (UserSessionModel)httpContext.Session["userSession"];
            if (userSession !=null && !string.IsNullOrWhiteSpace(userSession.AccessToken))
                return true;
            else
                return false;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary(
                            new
                            {
                                controller = "Login",
                                action = "Index"
                            })
                        );
        }
    }
}