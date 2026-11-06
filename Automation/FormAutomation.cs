using OpenQA.Selenium;
using SelfHealingSelenium.Config;
using SelfHealingSelenium.Locators;

namespace SelfHealingSelenium.Automation;

public record CheckResult(bool Passed, string Message);

public static class FormAutomation
{
    public static List<CheckResult> Run(IWebDriver driver)
    {
        var results = new List<CheckResult>();

        var firstName = SelfHealingLocator.FindElement(driver, FieldDescriptors.FirstName);
        firstName.Clear();
        firstName.SendKeys(Settings.FirstName);
        var enteredFirstName = firstName.GetAttribute("value");
        results.Add(new CheckResult(
            enteredFirstName == Settings.FirstName,
            $"First Name is {(enteredFirstName == Settings.FirstName ? "verified" : "incorrect")}."));

        var gender = SelfHealingLocator.FindElementInSameForm(
            driver,
            FieldDescriptors.FirstName,
            FieldDescriptors.Gender);
        gender.Click();
        results.Add(new CheckResult(
            gender.Selected,
            $"Gender {(gender.Selected ? "is selected" : "was not selected")}: {Settings.Gender}."));

        var state = SelfHealingLocator.FindElementInSameForm(
            driver,
            FieldDescriptors.FirstName,
            FieldDescriptors.State);
        state.FindElement(By.XPath($".//option[@value='{Settings.State}']")).Click();
        var selectedState = state.GetAttribute("value");
        results.Add(new CheckResult(
            selectedState == Settings.State,
            $"State {(selectedState == Settings.State ? "is selected" : "was not selected")}: {selectedState ?? "(none)"}."));

        return results;
    }
}
