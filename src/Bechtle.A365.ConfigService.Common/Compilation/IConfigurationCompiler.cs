using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Parsing;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    public interface IConfigurationCompiler
    {
        /// <summary>
        ///     compile a big data-set from two separate components
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="referencer"></param>
        /// <param name="parser"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task<IDictionary<string, string>> Compile(IDictionary<string, string> repository,
                                                  IDictionary<string, string> referencer,
                                                  IConfigurationParser parser,
                                                  CompilationOptions options);
    }

    public struct CompilationOptions
    {
        public ReferenceOption References { get; set; }

        public int RecursionLimit { get; set; }

        public CompilationOptions(ReferenceOption references) : this(references, 100)
        {
        }

        public CompilationOptions(ReferenceOption references, int recursionLimit)
        {
            References = references;
            RecursionLimit = recursionLimit;
        }

        /// <summary>
        ///     Environment references Environment-Repository
        /// </summary>
        public static CompilationOptions EnvFromEnv => new CompilationOptions(ReferenceOption.AllowSelfReference |
                                                                              ReferenceOption.AllowRepositoryReference |
                                                                              ReferenceOption.AllowRecursiveReference);

        /// <summary>
        ///     Struct References Environment-Repository
        /// </summary>
        public static CompilationOptions StructFromEnv => new CompilationOptions(ReferenceOption.AllowRepositoryReference |
                                                                                 ReferenceOption.AllowRecursiveReference);
    }

    [Flags]
    public enum ReferenceOption : byte
    {
        /// <summary>
        ///     references are not allowed
        /// </summary>
        None = 0,

        /// <summary>
        ///     collection may reference own keys
        /// </summary>
        AllowSelfReference = 1 << 0,

        /// <summary>
        ///     collection may reference its repository
        /// </summary>
        AllowRepositoryReference = 1 << 1,

        /// <summary>
        ///     references may be recursive, and are followed as long as other rules are followed (recursion depth, other flags)
        /// </summary>
        AllowRecursiveReference = 1 << 2,

        /// <summary>
        ///     set every flag in this Enum
        /// </summary>
        AllowAll = byte.MaxValue
    }
}