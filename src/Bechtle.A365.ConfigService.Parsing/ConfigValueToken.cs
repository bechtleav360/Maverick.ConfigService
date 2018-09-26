namespace Bechtle.A365.ConfigService.Parsing
{
    public enum ConfigValueToken
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