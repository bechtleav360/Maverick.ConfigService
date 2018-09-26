namespace Bechtle.A365.ConfigService.Parsing
{
    public class ValuePart : ConfigValuePart
    {
        public string Text { get; }

        public ValuePart(string text)
        {
            Text = text;
        }
    }
}