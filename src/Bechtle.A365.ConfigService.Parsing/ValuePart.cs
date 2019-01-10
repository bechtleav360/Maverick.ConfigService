namespace Bechtle.A365.ConfigService.Parsing
{
    public class ValuePart : ConfigValuePart
    {
        public ValuePart(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }
}