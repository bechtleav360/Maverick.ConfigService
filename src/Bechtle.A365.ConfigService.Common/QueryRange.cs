namespace Bechtle.A365.ConfigService.Common
{
    /// <summary>
    ///     range of items to query from the database
    /// </summary>
    public struct QueryRange
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
        ///     make a new <see cref="QueryRange"/> and override its parameters where necessary - otherwise create an all-inclusive range
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static QueryRange Make(int offset, int length) => new QueryRange(offset < 0 ? 0 : offset,
                                                                                length <= 0 ? int.MaxValue : length);
    }
}