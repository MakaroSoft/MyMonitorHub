using System;
using MyMonitorHub.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MyMonitorHub.Domain.Util
{
    public class CustomExceptionFilter : IExceptionFilter
    {
        private readonly ITempDataDictionaryFactory _tempDataFactory;

        public CustomExceptionFilter(ITempDataDictionaryFactory tempDataFactory)
        {
            _tempDataFactory = tempDataFactory;
        }

        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException(nameof(filterContext));

            if (filterContext.ExceptionHandled)
                return;

            if (filterContext.Exception is OrganizationException)
            {
                var controllerName = filterContext.RouteData.Values["controller"]?.ToString() ?? string.Empty;
                var actionName = filterContext.RouteData.Values["action"]?.ToString() ?? string.Empty;

                filterContext.Result = new ViewResult
                {
                    ViewName = "OrganizationError",
                    ViewData = new ViewDataDictionary<string>(
                        new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                        new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                    {
                        Model = $"{controllerName}.{actionName}"
                    }
                };
                filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.StatusCode = 500;
            }
        }
    }
}
