using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BusinessRules.UI.Common
{
    //Taken from https://stackoverflow.com/questions/59301912/rendering-view-to-string-in-core-3-0-could-not-find-an-irouter-associated-with
    public interface IRazorPartialToStringRenderer
    {
        Task<string> RenderToString(string viewName, object model, ViewDataDictionary viewData = null);
    }
}
