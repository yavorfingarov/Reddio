using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Reddio.Pages;

namespace Reddio.UnitTests.Pages
{
    public class ErrorTests
    {
        [Theory]
        [InlineData(400, "Bad Request")]
        [InlineData(404, "Not Found")]
        [InlineData(418, "Unexpected Error")]
        [InlineData(500, "Internal Server Error")]
        [InlineData(503, "Service Unavailable")]
        public void OnGet_PopulatesModel(int statusCode, string expectedErrorMessage)
        {
            var errorModel = CreateModel(statusCode);

            errorModel.OnGet();

            Assert.Equal(statusCode, errorModel.Response.StatusCode);
            Assert.Equal(expectedErrorMessage, errorModel.ErrorMessage);
        }

        [Fact]
        public void OnGet_ChangesStatusCode_WhenStatusCodeIs200()
        {
            var errorModel = CreateModel(200);

            errorModel.OnGet();

            Assert.Equal(404, errorModel.Response.StatusCode);
            Assert.Equal("Not Found", errorModel.ErrorMessage);
        }

        private static ErrorModel CreateModel(int statusCode)
        {
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
            var modelMetadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            var pageContext = new PageContext(actionContext)
            {
                ViewData = viewData
            };
            var errorModel = new ErrorModel()
            {
                PageContext = pageContext,
                TempData = tempData,
                Url = new UrlHelper(actionContext)
            };
            errorModel.Response.StatusCode = statusCode;

            return errorModel;
        }
    }
}
