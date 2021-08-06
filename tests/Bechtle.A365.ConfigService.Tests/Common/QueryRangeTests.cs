using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    public class QueryRangeTests
    {
        public static IEnumerable<object[]> QueryData => new[]
        {
            new object[] { 0, 0 },
            new object[] { 0, int.MinValue },
            new object[] { 0, int.MaxValue },
            new object[] { int.MinValue, 0 },
            new object[] { int.MaxValue, 0 },
            new object[] { int.MinValue, int.MinValue },
            new object[] { int.MinValue, int.MaxValue },
            new object[] { int.MaxValue, int.MaxValue },
            new object[] { int.MaxValue, int.MinValue }
        };

        [Fact]
        public void AllIsValid()
        {
            QueryRange queryRange = QueryRange.All;

            Assert.Equal(0, queryRange.Offset);
            Assert.Equal(int.MaxValue, queryRange.Length);
        }

        // we only check if the constructor throws up
        // ReSharper disable once ObjectCreationAsStatement
        [Theory]
        [MemberData(nameof(QueryData))]
        public void CreateNew(int offset, int length) => new QueryRange(offset, length);

        // we only check if the constructor throws up
        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void CreateNewBasic()
            => new QueryRange();

        [Theory]
        [MemberData(nameof(QueryData))]
        public void Equality(int offset, int length)
        {
            var left = new QueryRange(offset, length);
            var right = new QueryRange(offset, length);

            Assert.True(left.Equals(right));
            Assert.True(left == right);
            Assert.False(left != right);
        }

        [Theory]
        [MemberData(nameof(QueryData))]
        public void GetHashCodeStable(int offset, int length)
        {
            var range = new QueryRange(offset, length);

            List<int> hashes = Enumerable.Range(0, 1000)
                                         .Select(i => range.GetHashCode())
                                         .ToList();

            int example = range.GetHashCode();

            Assert.True(hashes.All(h => h == example), "hashes.All(h => h == example)");
        }

        [Theory]
        [MemberData(nameof(QueryData))]
        public void MakeSanitizesLength(int offset, int length)
            => Assert.InRange(QueryRange.Make(offset, length).Length, 1, int.MaxValue);

        [Theory]
        [MemberData(nameof(QueryData))]
        public void NoToStringExceptions(int offset, int length)
            => Assert.NotNull(new QueryRange(offset, length).ToString());
    }
}
