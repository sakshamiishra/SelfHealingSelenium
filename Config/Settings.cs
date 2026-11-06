namespace SelfHealingSelenium.Config;

public static class Settings
{
    public const string DefaultUrl = "https://app.cloudqa.io/home/AutomationPracticeForm";

    public static string Url =>
        Environment.GetEnvironmentVariable("CLOUDQA_URL") ?? DefaultUrl;

    public const string FirstName = "Saksham";
    public const string Gender = "Male";
    public const string State = "India";
}
