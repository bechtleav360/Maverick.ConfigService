using System;
using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common;
using Xunit;

namespace Bechtle.A365.ConfigService.Tests.Common
{
    public class ResultTests
    {
        public static IEnumerable<object?[]> ResultData => new[]
        {
            new object?[] { ErrorCode.None, true, "" },
            new object?[] { ErrorCode.None, true, null },
            new object?[] { ErrorCode.None, true, "FooBar" },
            new object?[] { ErrorCode.None, false, "" },
            new object?[] { ErrorCode.None, false, null },
            new object?[] { ErrorCode.None, false, "FooBar" },
            new object?[] { ErrorCode.Undefined, true, "" },
            new object?[] { ErrorCode.Undefined, true, null },
            new object?[] { ErrorCode.Undefined, true, "FooBar" },
            new object?[] { ErrorCode.Undefined, false, "" },
            new object?[] { ErrorCode.Undefined, false, null },
            new object?[] { ErrorCode.Undefined, false, "FooBar" },
            new object?[] { ErrorCode.EnvironmentAlreadyExists, true, "" },
            new object?[] { ErrorCode.EnvironmentAlreadyExists, true, null },
            new object?[] { ErrorCode.EnvironmentAlreadyExists, true, "FooBar" },
            new object?[] { ErrorCode.EnvironmentAlreadyExists, false, "" },
            new object?[] { ErrorCode.EnvironmentAlreadyExists, false, null },
            new object?[] { ErrorCode.EnvironmentAlreadyExists, false, "FooBar" },
            new object?[] { ErrorCode.StructureAlreadyExists, true, "" },
            new object?[] { ErrorCode.StructureAlreadyExists, true, null },
            new object?[] { ErrorCode.StructureAlreadyExists, true, "FooBar" },
            new object?[] { ErrorCode.StructureAlreadyExists, false, "" },
            new object?[] { ErrorCode.StructureAlreadyExists, false, null },
            new object?[] { ErrorCode.StructureAlreadyExists, false, "FooBar" },
            new object?[] { ErrorCode.DbUpdateError, true, "" },
            new object?[] { ErrorCode.DbUpdateError, true, null },
            new object?[] { ErrorCode.DbUpdateError, true, "FooBar" },
            new object?[] { ErrorCode.DbUpdateError, false, "" },
            new object?[] { ErrorCode.DbUpdateError, false, null },
            new object?[] { ErrorCode.DbUpdateError, false, "FooBar" },
            new object?[] { ErrorCode.NotFound, true, "" },
            new object?[] { ErrorCode.NotFound, true, null },
            new object?[] { ErrorCode.NotFound, true, "FooBar" },
            new object?[] { ErrorCode.NotFound, false, "" },
            new object?[] { ErrorCode.NotFound, false, null },
            new object?[] { ErrorCode.NotFound, false, "FooBar" },
            new object?[] { ErrorCode.InvalidData, true, "" },
            new object?[] { ErrorCode.InvalidData, true, null },
            new object?[] { ErrorCode.InvalidData, true, "FooBar" },
            new object?[] { ErrorCode.InvalidData, false, "" },
            new object?[] { ErrorCode.InvalidData, false, null },
            new object?[] { ErrorCode.InvalidData, false, "FooBar" },
            new object?[] { ErrorCode.DefaultEnvironmentAlreadyExists, true, "" },
            new object?[] { ErrorCode.DefaultEnvironmentAlreadyExists, true, null },
            new object?[] { ErrorCode.DefaultEnvironmentAlreadyExists, true, "FooBar" },
            new object?[] { ErrorCode.DefaultEnvironmentAlreadyExists, false, "" },
            new object?[] { ErrorCode.DefaultEnvironmentAlreadyExists, false, null },
            new object?[] { ErrorCode.DefaultEnvironmentAlreadyExists, false, "FooBar" },
            new object?[] { ErrorCode.DbQueryError, true, "" },
            new object?[] { ErrorCode.DbQueryError, true, null },
            new object?[] { ErrorCode.DbQueryError, true, "FooBar" },
            new object?[] { ErrorCode.DbQueryError, false, "" },
            new object?[] { ErrorCode.DbQueryError, false, null },
            new object?[] { ErrorCode.DbQueryError, false, "FooBar" },
            new object?[] { ErrorCode.ValidationFailed, true, "" },
            new object?[] { ErrorCode.ValidationFailed, true, null },
            new object?[] { ErrorCode.ValidationFailed, true, "FooBar" },
            new object?[] { ErrorCode.ValidationFailed, false, "" },
            new object?[] { ErrorCode.ValidationFailed, false, null },
            new object?[] { ErrorCode.ValidationFailed, false, "FooBar" },
            new object?[] { ErrorCode.EnvironmentAlreadyDeleted, true, "" },
            new object?[] { ErrorCode.EnvironmentAlreadyDeleted, true, null },
            new object?[] { ErrorCode.EnvironmentAlreadyDeleted, true, "FooBar" },
            new object?[] { ErrorCode.EnvironmentAlreadyDeleted, false, "" },
            new object?[] { ErrorCode.EnvironmentAlreadyDeleted, false, null },
            new object?[] { ErrorCode.EnvironmentAlreadyDeleted, false, "FooBar" },
            new object?[] { ErrorCode.FailedToRetrieveItem, true, "" },
            new object?[] { ErrorCode.FailedToRetrieveItem, true, null },
            new object?[] { ErrorCode.FailedToRetrieveItem, true, "FooBar" },
            new object?[] { ErrorCode.FailedToRetrieveItem, false, "" },
            new object?[] { ErrorCode.FailedToRetrieveItem, false, null },
            new object?[] { ErrorCode.FailedToRetrieveItem, false, "FooBar" }
        };

        // we only check if the constructor throws up
        // ReSharper disable once ObjectCreationAsStatement
        [Theory]
        [MemberData(nameof(ResultData))]
        public void AssignValues(ErrorCode code, bool error, string message) => new Result
        {
            Code = code,
            IsError = error,
            Message = message
        };

        // we only check if the constructor throws up
        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void NoExceptionsInDefaultConstructor() => new Result();

        [Theory]
        [MemberData(nameof(ResultData))]
        public void NoExceptionsInError(ErrorCode code, bool error, string message)
        {
            // shut-up about unused parameters
            Assert.Equal(error, error);
            Result.Error(message, code);
        }

        [Fact]
        public void NoExceptionsInSuccess() => Result.Success();

        [Fact]
        public void NoExceptionsInSuccessWithData() => Result.Success(DateTime.UtcNow);

        // we only check if the constructor throws up
        // ReSharper disable once ObjectCreationAsStatement
        [Fact]
        public void NoExceptionsInTypedDefaultConstructor() => new Result<string>();

        [Theory]
        [MemberData(nameof(ResultData))]
        public void NoExceptionsInTypedError(ErrorCode code, bool error, string message)
        {
            // shut-up about unused parameters
            Assert.Equal(error, error);
            Result.Error(message, code, DateTime.UtcNow);
        }

        [Theory]
        [MemberData(nameof(ResultData))]
        public void NoExceptionsInTypedErrorWithoutData(ErrorCode code, bool error, string message)
        {
            // shut-up about unused parameters
            Assert.Equal(error, error);
            Result.Error<DateTime>(message, code);
        }

        [Theory]
        [MemberData(nameof(ResultData))]
        public void ValuesAssignedInError(ErrorCode code, bool error, string message)
        {
            // shut-up about unused parameters
            Assert.Equal(error, error);

            IResult result = Result.Error(message, code);

            Assert.True(result.IsError);
            Assert.Equal(code, result.Code);
            Assert.Equal(message, result.Message);
        }

        [Fact]
        public void ValuesAssignedInSuccess()
        {
            DateTime data = DateTime.UtcNow;

            IResult<DateTime> result = Result.Success(data);

            Assert.False(result.IsError);
            Assert.Equal(ErrorCode.None, result.Code);
            Assert.Equal(string.Empty, result.Message);
            Assert.Equal(data, result.Data);
        }

        [Theory]
        [MemberData(nameof(ResultData))]
        public void ValuesAssignedInTypedError(ErrorCode code, bool error, string message)
        {
            // shut-up about unused parameters
            Assert.Equal(error, error);

            DateTime data = DateTime.UtcNow;

            IResult<DateTime> result = Result.Error(message, code, data);

            Assert.True(result.IsError);
            Assert.Equal(code, result.Code);
            Assert.Equal(message, result.Message);
            Assert.Equal(data, result.Data);
        }

        [Theory]
        [MemberData(nameof(ResultData))]
        public void ValuesAssignedInTypedErrorWithoutData(ErrorCode code, bool error, string message)
        {
            // shut-up about unused parameters
            Assert.Equal(error, error);

            IResult<DateTime> result = Result.Error<DateTime>(message, code);

            Assert.True(result.IsError);
            Assert.Equal(code, result.Code);
            Assert.Equal(message, result.Message);
            Assert.Equal(default, result.Data);
        }
    }
}
