using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SelfHealingSelenium.Automation;
using SelfHealingSelenium.Config;

IWebDriver? driver = null;

try
{
    driver = new ChromeDriver();
    driver.Navigate().GoToUrl(Settings.Url);

    var results = FormAutomation.Run(driver);
    foreach (var result in results)
    {
        Console.WriteLine($"{(result.Passed ? "PASS" : "FAIL")}: {result.Message}");
    }

    if (results.Any(result => !result.Passed))
    {
        Environment.ExitCode = 1;
    }
}
catch (Exception exception)
{
    Console.WriteLine($"FAIL: Automation could not complete. {exception.Message}");
    Environment.ExitCode = 1;
}
finally
{
    driver?.Quit();
}
