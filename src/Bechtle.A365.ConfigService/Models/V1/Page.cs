using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;

namespace Bechtle.A365.ConfigService.Models.V1
{
    /// <summary>
    ///     Paged data
    /// </summary>
    /// <typeparam name="T">items contained in this Page</typeparam>
    public class Page<T> : IEnumerable<T>
    {
        /// <summary>
        ///     Number of items returned in this Page
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        ///     List of items returned in this Page
        /// </summary>
        public IList<T> Items { get; set; }

        /// <summary>
        ///     Offset from 0 of items returned in this Page
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        ///     Total number of items in the collection
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        ///     Create a new empty instance of <see cref="Page{T}" />
        /// </summary>
        public Page()
        {
            Count = 0;
            Items = new List<T>();
            Offset = 0;
            TotalCount = 0;
        }

        /// <summary>
        ///     Create a new instance of <see cref="Page{T}" /> filled with the given items, with <see cref="Offset" />=0
        /// </summary>
        /// <param name="items">List of items this Page contains</param>
        public Page(IList<T> items)
        {
            Count = items.Count;
            Items = items;
            Offset = 0;
            TotalCount = 0;
        }

        /// <summary>
        ///     Create a new instance of <see cref="Page{T}" /> filled with the given items, with <see cref="Offset" />=0
        /// </summary>
        /// <param name="items">List of items this Page contains</param>
        public Page(IEnumerable<T> items)
        {
            Items = items.ToList();
            Count = Items.Count;
            Offset = 0;
            TotalCount = 0;
        }

        /// <summary>
        ///     Create a new instance of <see cref="Page{T}" /> filled with the given items,
        ///     with <see cref="Offset" /> and <see cref="TotalCount" /> set to the given values
        /// </summary>
        /// <param name="items">List of items this Page contains</param>
        /// <param name="offset">offset from 0 that this page represents</param>
        /// <param name="totalCount">total number of items contained in the source-collection</param>
        public Page(IList<T> items, int offset, int totalCount)
        {
            Count = items.Count;
            Items = items;
            Offset = offset;
            TotalCount = totalCount;
        }

        /// <summary>
        ///     Create a new instance of <see cref="Page{T}" /> filled with the given items,
        ///     with <see cref="Offset" /> and <see cref="TotalCount" /> set to the given values
        /// </summary>
        /// <param name="items">List of items this Page contains</param>
        /// <param name="range">range to take offset from</param>
        /// <param name="totalCount">total number of items contained in the source-collection</param>
        public Page(IList<T> items, QueryRange range, int totalCount)
        {
            Count = items.Count;
            Items = items;
            Offset = range.Offset;
            TotalCount = totalCount;
        }

        /// <summary>
        ///     Create a new instance of <see cref="Page{T}" /> filled with the given items,
        ///     with <see cref="Offset" /> and <see cref="TotalCount" /> set to the given values
        /// </summary>
        /// <param name="items">List of items this Page contains</param>
        /// <param name="offset">offset from 0 that this page represents</param>
        /// <param name="totalCount">total number of items contained in the source-collection</param>
        public Page(IEnumerable<T> items, int offset, int totalCount)
        {
            Items = items.ToList();
            Count = Items.Count;
            Offset = offset;
            TotalCount = totalCount;
        }

        /// <summary>
        ///     Create a new instance of <see cref="Page{T}" /> filled with the given items,
        ///     with <see cref="Offset" /> and <see cref="TotalCount" /> set to the given values
        /// </summary>
        /// <param name="items">List of items this Page contains</param>
        /// <param name="range">range to take offset from</param>
        /// <param name="totalCount">total number of items contained in the source-collection</param>
        public Page(IEnumerable<T> items, QueryRange range, int totalCount)
        {
            Items = items.ToList();
            Count = Items.Count;
            Offset = range.Offset;
            TotalCount = totalCount;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Items).GetEnumerator();
    }
}
