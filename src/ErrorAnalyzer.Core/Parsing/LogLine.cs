namespace ErrorAnalyzer.Core.Parsing;

internal sealed class LogLine
{
    public LogLine()
    {
        Text = string.Empty;
    }

    public LogLine(int number, string text)
    {
        Number = number;
        Text = text;
    }

    public int Number { get; set; }

    public string Text { get; set; }
}
