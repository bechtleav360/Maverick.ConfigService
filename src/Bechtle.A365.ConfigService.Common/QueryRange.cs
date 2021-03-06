﻿using System;

namespace Bechtle.A365.ConfigService.Common
{
    /// <summary>
    ///     range of items to query from the database
    /// </summary>
    public struct QueryRange : IEquatable<QueryRange>
    {
        /// <summary>
        ///     select items after this offset using the current ordering
        /// </summary>
        public int Offset { get; }

        /// <summary>
        ///     take this amount of items
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public QueryRange(int offset, int length)
        {
            Offset = offset;
            Length = length;
        }

        /// <summary>
        ///     undefined range - should include all found data
        /// </summary>
        public static readonly QueryRange All = new QueryRange(0, int.MaxValue);

        /// <summary>
        ///     make a new <see cref="QueryRange" /> and override its parameters where necessary - otherwise create an all-inclusive range
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static QueryRange Make(int offset, int length) => new QueryRange(offset < 0 ? 0 : offset,
                                                                                length <= 0 ? int.MaxValue : length);

        public bool Equals(QueryRange other) => Offset == other.Offset && Length == other.Length;

        public override bool Equals(object obj) => obj is QueryRange other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Offset * 397) ^ Length;
            }
        }

        public static bool operator ==(QueryRange left, QueryRange right) => left.Equals(right);

        public static bool operator !=(QueryRange left, QueryRange right) => !left.Equals(right);

        public override string ToString() => $"[{nameof(QueryRange)}; {nameof(Offset)}: {Offset}; {nameof(Length)}: {Length}]";
    }
}