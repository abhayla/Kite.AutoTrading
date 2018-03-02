using Kite.AutoTrading.Filters;
using System.Web.Mvc;

namespace Kite.AutoTrading.Controllers
{
    [AuthorizeUser]
    public class BaseController : Controller
    {
        public BaseController()
        {
            
        }
    }
}