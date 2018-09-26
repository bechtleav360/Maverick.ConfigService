namespace Bechtle.A365.ConfigService.Parsing
{
    internal enum ConfigValueToken
    {
        None,
        Value,
        CommandValue,
        CommandKeyword,
        InstructionOpen,
        InstructionClose,
        InstructionSeparator
    }
}