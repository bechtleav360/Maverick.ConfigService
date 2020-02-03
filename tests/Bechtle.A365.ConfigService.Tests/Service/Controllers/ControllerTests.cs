using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Service.Controllers
{
    public abstract class ControllerTests<TController>
    {
        protected abstract TController CreateController();

        protected async Task<TActionResult> TestAction<TActionResult>(Func<TController, Task<IActionResult>> actionInvoker,
                                                                      Action<TController> setupAction = null)
        {
            var controller = CreateController();

            setupAction?.Invoke(controller);

            var result = await actionInvoker(controller);

            Assert.IsType<TActionResult>(result);

            return (TActionResult) result;
        }

        protected TActionResult TestAction<TActionResult>(Func<TController, IActionResult> actionInvoker, Action<TController> setupAction = null)
        {
            var controller = CreateController();

            setupAction?.Invoke(controller);

            var result = actionInvoker(controller);

            Assert.IsType<TActionResult>(result);

            return (TActionResult) result;
        }
    }
}