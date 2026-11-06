using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

IWebDriver driver = new ChromeDriver();
try
{
    driver.Navigate().GoToUrl(
        "https://app.cloudqa.io/home/AutomationPracticeForm"
    );
    Console.WriteLine("Page Title: " + driver.Title);
    Console.WriteLine("Browser launched successfully!");
    Console.ReadLine();
}
finally
{
    driver.Quit();
}