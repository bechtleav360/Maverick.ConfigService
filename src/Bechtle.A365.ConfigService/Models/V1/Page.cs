using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Models.V1
{
    /// <summary>
    ///     Paged data
    /// </summary>
    /// <typeparam name="T">items contained in this Page</typeparam>
    public class Page<T>
    {
        /// <summary>
        ///     List of items returned in this Page
        /// </summary>
        public IList<T> Items { get; set; }

        /// <summary>
        ///     Number of items returned in this Page
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        ///     Offset from 0 of items returned in this Page
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        ///     Total number of items in the collection
        /// </summary>
        public int TotalLength { get; set; }
    }
}
