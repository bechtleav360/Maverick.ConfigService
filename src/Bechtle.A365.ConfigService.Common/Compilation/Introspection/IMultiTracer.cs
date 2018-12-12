namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection
{
    public interface IMultiTracer : ITracer
    {
        /// <summary>
        ///     add a new sub-tracer with the result of a path-resolution that produced only one value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        ITracer AddPathResult(string value);

        /// <summary>
        ///     add a new sub-tracer with the result of a path-resolution that produced multiple values each with their own key
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        ITracer AddPathResult(string path, string value);
    }
}