using Kite.AutoTrading.Common.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Kite.AutoTrading.Filters
{
    public class GlobalExceptionFilter : HandleErrorAttribute
    {

        public override void OnException(ExceptionContext filterContext)
        {
            //if (filterContext.ExceptionHandled || !filterContext.HttpContext.IsCustomErrorEnabled)
            //{
            //    return;
            //}

            // if the request is AJAX return JSON else view.
            if (IsAjax(filterContext))
            {
                //Because its a exception raised after Ajax invocation
                //Lets return Json
                filterContext.Result = new JsonResult()
                {
                    Data = filterContext.Exception.Message,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };

                filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.Clear();
            }
            else
            {
                //Normal Exception
                //So let it handle by its default ways.
                base.OnException(filterContext);
            }

            string errorMessage = Environment.NewLine;

            try
            {
                //Get all the info we need to define where the error occured and with what data
                var param = new NameValueCollection { filterContext.RequestContext.HttpContext.Request.Form, filterContext.RequestContext.HttpContext.Request.QueryString };
                var controller = filterContext.RouteData.Values["controller"].ToString();
                var action = filterContext.RouteData.Values["action"].ToString();
                //var signature = filterContext.Controller.GetType().GetMethod(action).ToString();


                errorMessage += string.Format("Controller: {0}", controller) + Environment.NewLine;
                errorMessage += string.Format("Action: {0}", action) + Environment.NewLine;
                //errorMessage += string.Format("Signature: {0}", signature) + Environment.NewLine + Environment.NewLine + Environment.NewLine + Environment.NewLine;
                errorMessage += JsonConvert.SerializeObject(filterContext.Exception);
                //foreach (var key in param.AllKeys)
                //{
                //    errorMessage += string.Format("Key: {0} = {1}", key, param[key]) + Environment.NewLine ;
                //}
                //error logging
                ApplicationLogger.LogException(errorMessage);
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogException(JsonConvert.SerializeObject(ex));
            }

        }

        private bool IsAjax(ExceptionContext filterContext)
        {
            return filterContext.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}