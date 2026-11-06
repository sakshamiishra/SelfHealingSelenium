namespace SelfHealingSelenium.Locators;

public class FieldDescriptor
{
    public string FieldName { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Placeholder { get; set; }
    public string? Type { get; set; }
    public string? Value { get; set; }
}

public static class FieldDescriptors
{
    public static readonly FieldDescriptor FirstName = new()
    {
        FieldName = "First Name",
        TagName = "input",
        Id = "fname",
        Name = "First Name",
        Placeholder = "Name",
        Type = "text"
    };

    public static readonly FieldDescriptor Gender = new()
    {
        FieldName = "Gender",
        TagName = "input",
        Id = "male",
        Name = "gender",
        Type = "radio",
        Value = "Male"
    };

    public static readonly FieldDescriptor State = new()
    {
        FieldName = "State",
        TagName = "select",
        Id = "state",
        Name = "State"
    };
}
