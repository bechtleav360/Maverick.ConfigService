using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bechtle.A365.ConfigService.Common
{
    /// <summary>
    ///     custom contract resolver based on other <see cref="IContractResolver"/>, but implementing additional rules
    /// </summary>
    public class OrderedContractResolver : CamelCasePropertyNamesContractResolver
    {
        private static readonly Dictionary<Type, int> TypeOrder = new Dictionary<Type, int>
        {
            {typeof(bool), 0},
            {typeof(short), 1},
            {typeof(int), 2},
            {typeof(long), 3},
            {typeof(float), 4},
            {typeof(double), 5},
            {typeof(byte), 6},
            {typeof(char), 7},
            {typeof(string), 8},
            {typeof(void), 9},
        };

        /// <inheritdoc />
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            => base.CreateProperties(type, memberSerialization)
                   // order by type
                   .OrderBy(p => TypeOrder.ContainsKey(p.PropertyType)
                                     ? TypeOrder[p.PropertyType]
                                     : TypeOrder[typeof(void)])
                   // then by name  ascending
                   .ThenBy(p => p.PropertyName)
                   .ToList();
    }
}