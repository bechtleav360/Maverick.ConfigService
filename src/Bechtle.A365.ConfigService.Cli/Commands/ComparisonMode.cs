using System;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    // ReSharper disable once ShiftExpressionRealShiftCountIsZero
    [Flags]
    public enum ComparisonMode : byte
    {
        Add = 1 << 0,
        Delete = 1 << 1,
        Match = Add | Delete
    }
}