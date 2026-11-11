namespace UnionTypes.Demo;

public union NumberOrText(int, string);

public static class LanguageExamples
{
    public static void Run(TextWriter output)
    {
        Description converted = new PlainText("implicit conversion");
        output.WriteLine(Descriptions.Render(converted));
        NumberOrText number = 42;
        output.WriteLine(number switch
        {
            int value => $"int: {value}",
            string value => $"string: {value}"
        });
        output.WriteLine($"boxed type: {number.Value?.GetType().FullName}");
        NumberOrText empty = default;
        output.WriteLine(empty switch
        {
            int value => $"int: {value}",
            string value => $"string: {value}",
            null => "default union: null"
        });
    }
}
