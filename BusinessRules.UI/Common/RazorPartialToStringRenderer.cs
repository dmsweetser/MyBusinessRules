using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Diagnostics.CodeAnalysis;
namespace BusinessRules.UI.Common
{
    //Taken from https://stackoverflow.com/questions/59301912/rendering-view-to-string-in-core-3-0-could-not-find-an-irouter-associated-with
    [ExcludeFromCodeCoverage]
    public class RazorPartialToStringRenderer : IRazorPartialToStringRenderer
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IHttpContextAccessor _contextAccessor;

        public RazorPartialToStringRenderer(IRazorViewEngine razorViewEngine,
                                 ITempDataProvider tempDataProvider,
                                 IHttpContextAccessor contextAccessor)
        {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _contextAccessor = contextAccessor;
        }

        public async Task<string> RenderToString(string viewName, object model, ViewDataDictionary viewData = null)
        {
            var actionContext = new ActionContext(_contextAccessor.HttpContext, _contextAccessor.HttpContext.GetRouteData(), new ActionDescriptor());

            await using var sw = new StringWriter();
            var viewResult = FindView(actionContext, viewName);

            if (viewResult == null)
            {
                throw new ArgumentNullException($"{viewName} does not match any available view");
            }

            viewData ??= new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
            viewData.Model = model;

            var viewContext = new ViewContext(
                actionContext,
                viewResult,
                viewData,
                new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.RenderAsync(viewContext);
            return sw.ToString();
        }


        private IView FindView(ActionContext actionContext, string viewName)
        {
            var getViewResult = _razorViewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true);
            if (getViewResult.Success)
            {
                return getViewResult.View;
            }

            var findViewResult = _razorViewEngine.FindView(actionContext, viewName, isMainPage: true);
            if (findViewResult.Success)
            {
                return findViewResult.View;
            }

            var searchedLocations = getViewResult.SearchedLocations.Concat(findViewResult.SearchedLocations);
            var errorMessage = string.Join(
                Environment.NewLine,
                new[] { $"Unable to find view '{viewName}'. The following locations were searched:" }.Concat(searchedLocations));

            throw new InvalidOperationException(errorMessage);
        }
    }
}
