using OpenQA.Selenium;

namespace SelfHealingSelenium.Locators;

public static class SelfHealingLocator
{
    // An exact ID is strong evidence, while name and placeholder are also useful
    // when an ID changes. Tag and type help confirm the kind of control.
    private const int IdWeight = 10;
    private const int NameWeight = 14;
    private const int PlaceholderWeight = 8;
    private const int AssociatedLabelWeight = 10;
    private const int TagNameWeight = 2;
    private const int TypeWeight = 2;

    // A candidate needs identity evidence plus a minimum score. The margin avoids
    // choosing between two candidates whose scores are too close to distinguish.
    private const int MinimumReliableScore = 10;
    private const int RequiredScoreMargin = 3;

    public static IWebElement FindElement(IWebDriver driver, FieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(driver);
        return FindElement((ISearchContext)driver, descriptor, (IJavaScriptExecutor)driver);
    }

    public static IWebElement FindElement(ISearchContext searchContext, FieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(searchContext);
        ArgumentNullException.ThrowIfNull(descriptor);
        return FindElement(searchContext, descriptor, searchContext as IJavaScriptExecutor);
    }

    public static IWebElement FindElement(
        IWebDriver driver,
        ISearchContext searchContext,
        FieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(searchContext);
        ArgumentNullException.ThrowIfNull(descriptor);
        return FindElement(searchContext, descriptor, (IJavaScriptExecutor)driver);
    }

    private static IWebElement FindElement(
        ISearchContext searchContext,
        FieldDescriptor descriptor,
        IJavaScriptExecutor? javaScriptExecutor)
    {
        ArgumentNullException.ThrowIfNull(searchContext);
        ArgumentNullException.ThrowIfNull(descriptor);

        if (string.IsNullOrWhiteSpace(descriptor.TagName))
        {
            throw new ArgumentException("The field descriptor must include a tag name.", nameof(descriptor));
        }

        var matches = new List<(IWebElement Element, int Score, string[] Evidence, bool HasLabelMatch)>();
        var candidates = searchContext.FindElements(By.TagName(descriptor.TagName));

        foreach (var candidate in candidates)
        {
            if (!candidate.Displayed || !candidate.Enabled)
            {
                continue;
            }

            var candidateType = candidate.GetAttribute("type");
            if (!string.IsNullOrWhiteSpace(descriptor.Type) &&
                !AttributeMatches(candidateType, descriptor.Type))
            {
                continue;
            }

            // Value confirms a particular option (such as Male) within an
            // already identified field group; it cannot replace identity evidence.
            if (!string.IsNullOrWhiteSpace(descriptor.Value) &&
                !AttributeMatches(candidate.GetAttribute("value"), descriptor.Value))
            {
                continue;
            }

            var score = TagNameWeight;
            var evidence = new List<string> { "tag name" };

            if (!string.IsNullOrWhiteSpace(descriptor.Type))
            {
                score += TypeWeight;
                evidence.Add("type");
            }

            var identityMatches = 0;

            AddAttributeEvidence(candidate, "id", descriptor.Id, IdWeight, ref score, evidence, ref identityMatches);
            AddAttributeEvidence(candidate, "name", descriptor.Name, NameWeight, ref score, evidence, ref identityMatches);
            AddAttributeEvidence(candidate, "placeholder", descriptor.Placeholder, PlaceholderWeight, ref score, evidence, ref identityMatches);

            var hasLabelMatch = HasMatchingAssociatedLabel(javaScriptExecutor, candidate, descriptor.FieldName);
            if (descriptor.RequireAssociatedLabel && !hasLabelMatch)
            {
                continue;
            }

            if (hasLabelMatch)
            {
                score += AssociatedLabelWeight;
                evidence.Add("associated label text");
                identityMatches++;
            }

            // Tag and type alone describe a kind of control, not the requested field.
            if (identityMatches > 0)
            {
                matches.Add((candidate, score, evidence.ToArray(), hasLabelMatch));
            }
        }

        // An exact associated label is stronger semantic evidence than an
        // unlabeled duplicate. Keep only labeled candidates when any are found.
        if (matches.Any(match => match.HasLabelMatch))
        {
            matches = matches.Where(match => match.HasLabelMatch).ToList();
        }

        var rankedMatches = matches.OrderByDescending(match => match.Score).ToList();
        if (rankedMatches.Count == 0 || rankedMatches[0].Score < MinimumReliableScore)
        {
            var reason = descriptor.RequireAssociatedLabel
                ? $"No visible, enabled candidate has an associated label matching '{descriptor.FieldName}'."
                : $"No reliable visible, enabled match was found for field '{descriptor.FieldName}'.";
            throw new NoSuchElementException(
                reason);
        }

        var bestMatch = rankedMatches[0];
        if (rankedMatches.Count > 1 &&
            bestMatch.Score - rankedMatches[1].Score < RequiredScoreMargin)
        {
            throw new InvalidOperationException(
                $"The field '{descriptor.FieldName}' is ambiguous: the top candidates have scores " +
                $"{bestMatch.Score} and {rankedMatches[1].Score}. No element was selected.");
        }

        return bestMatch.Element;
    }

    public static IWebElement FindElementInSameForm(
        IWebDriver driver,
        FieldDescriptor anchorDescriptor,
        FieldDescriptor targetDescriptor)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(anchorDescriptor);
        ArgumentNullException.ThrowIfNull(targetDescriptor);

        var anchor = FindElement(driver, anchorDescriptor);
        IWebElement form;

        try
        {
            // Find the nearest owning form by DOM relationship, not by a form ID.
            form = anchor.FindElement(By.XPath("ancestor::form[1]"));
        }
        catch (NoSuchElementException)
        {
            throw new InvalidOperationException(
                $"The anchor field '{anchorDescriptor.FieldName}' is not inside a form.");
        }

        return FindElement(driver, (ISearchContext)form, targetDescriptor);
    }

    private static bool HasMatchingAssociatedLabel(
        IJavaScriptExecutor? javaScriptExecutor,
        IWebElement candidate,
        string fieldName)
    {
        if (javaScriptExecutor is null || string.IsNullOrWhiteSpace(fieldName))
        {
            return false;
        }

        const string script = @"
            const element = arguments[0];
            const normalize = value => String(value || '').replace(/\s+/g, ' ').trim().toLowerCase();
            const expected = normalize(arguments[1]);
            const labels = element && element.labels ? Array.from(element.labels) : [];
            const candidateForm = element && element.form;
            if (!candidateForm) return false;
            return labels.some(label =>
                normalize(label.textContent) === expected &&
                label.closest('form') === candidateForm);
        ";

        return javaScriptExecutor.ExecuteScript(script, candidate, fieldName) is true;
    }

    private static void AddAttributeEvidence(
        IWebElement candidate,
        string attributeName,
        string? expectedValue,
        int weight,
        ref int score,
        List<string> evidence,
        ref int identityMatches)
    {
        if (!string.IsNullOrWhiteSpace(expectedValue) &&
            AttributeMatches(candidate.GetAttribute(attributeName), expectedValue))
        {
            score += weight;
            evidence.Add(attributeName);
            identityMatches++;
        }
    }

    private static bool AttributeMatches(string? actualValue, string expectedValue)
    {
        return string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase);
    }
}
