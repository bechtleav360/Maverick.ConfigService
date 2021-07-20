using System;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    /// <summary>
    ///      flags indicating how a diff is generated
    /// </summary>
    // ReSharper disable once ShiftExpressionRealShiftCountIsZero
    [Flags]
    public enum ComparisonMode : byte
    {
        /// <summary>
        ///     Only generate additions for missing data (data added in source but not in target)
        /// </summary>
        Add = 1 << 0,

        /// <summary>
        ///     Only generate deletions for missing data (data deleted from source but not in target)
        /// </summary>
        Delete = 1 << 1,

        /// <summary>
        ///     Generate both Additions and Deletions to reach the target-state
        /// </summary>
        Match = Add | Delete
    }
}
