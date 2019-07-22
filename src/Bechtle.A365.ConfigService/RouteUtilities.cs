using Microsoft.AspNetCore.Mvc;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     utilities for routing between actions / controllers
    /// </summary>
    public static class RouteUtilities
    {
        /// <summary>
        ///     return the name of the given controller, but remove the "Controller" to function in re-routing
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string ControllerName<T>() where T : Controller
            => typeof(T).Name.Replace("Controller", "");
    }
}