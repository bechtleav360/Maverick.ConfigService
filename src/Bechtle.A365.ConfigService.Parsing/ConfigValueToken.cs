namespace Bechtle.A365.ConfigService.Parsing
{
    public enum ConfigValueToken
    {
        None,
        Fluff,
        Value,
        Keyword,
        InstructionOpen,
        InstructionClose,
        InstructionSeparator
    }
}