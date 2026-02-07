using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public enum TestEnum
{
    None,
    Upload,
    OwnerPolicy,
    ConsignorApproval,
    AutoApprove
}

public class TestClass
{
    public TestEnum EnumValue { get; set; }
}

class Program
{
    static void Main()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        foreach (TestEnum value in Enum.GetValues<TestEnum>())
        {
            var test = new TestClass { EnumValue = value };
            var json = JsonSerializer.Serialize(test, options);
            Console.WriteLine($"{value} -> {json}");
        }
    }
}
