using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Controllers
{
    public abstract class ControllerTests<TController>
    {
        protected abstract TController CreateController();

        protected async Task<TActionResult> TestAction<TActionResult>(
            Func<TController, Task<IActionResult>> actionInvoker)
        {
            var result = await actionInvoker(CreateController());

            Assert.IsType<TActionResult>(result);

            return (TActionResult) result;
        }
    }
}