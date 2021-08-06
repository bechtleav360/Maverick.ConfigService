using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Utilities
{
    public class RouteUtilitiesTests
    {
        [Fact]
        public void NoNullReturned()
        {
            Assert.NotNull(RouteUtilities.ControllerName<TestController>());
        }

        [Fact]
        public void StripControllerFromName()
        {
            Assert.Equal("Test", RouteUtilities.ControllerName<TestController>());
        }

        [Fact]
        public void SucceedWithWrongName()
        {
            Assert.Equal("", RouteUtilities.ControllerName<Controller>());
        }

        private class TestController : Microsoft.AspNetCore.Mvc.Controller
        {
        }

        private class Controller : Microsoft.AspNetCore.Mvc.Controller
        {
        }
    }
}
