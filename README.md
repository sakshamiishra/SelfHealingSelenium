# SelfHealingSelenium

A C# Selenium console application that automates three fields in the CloudQA practice form. It uses field descriptors and a heuristic locator to compare candidate elements, then scopes related fields to the form containing the First Name anchor.

The project is an educational first version. Its locator rules are explicit and explainable; they are not AI or machine learning, and they cannot recover from every HTML change.

## Table of Contents

- [Problem and objectives](#problem-and-objectives)
- [Technologies](#technologies)
- [Features and locator design](#features-and-locator-design)
- [Project structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Setup and run](#setup-and-run)
- [Configuration](#configuration)
- [Expected output](#expected-output)
- [Testing and mutation scenarios](#testing-and-mutation-scenarios)
- [Limitations and failure conditions](#limitations-and-failure-conditions)
- [Design decisions and future improvements](#design-decisions-and-future-improvements)

## Problem and objectives

Web forms often identify controls with attributes such as `id`, `name`, and `placeholder`. Those attributes can change as a page evolves. A test that depends on a single fixed selector may then stop finding its target, while a locator that simply picks the first similar control risks entering data into the wrong form.

This project explores a safer approach for the CloudQA Automation Practice Form:

- Enter `Saksham` in First Name.
- Select the Male radio option.
- Select `India` in State.
- Verify each operation and print a PASS or FAIL result.
- Reject ambiguous or insufficiently identified candidates instead of choosing arbitrarily.

The target URL defaults to `https://app.cloudqa.io/home/AutomationPracticeForm`.

## Technologies

- C# with a .NET 10 console application (`net10.0`).
- Selenium WebDriver 4.52.0.
- Google Chrome, launched as a WebDriver-controlled browser session.
- Selenium Manager: the code constructs `ChromeDriver` without a manually configured driver path. Selenium Manager is Selenium's default driver resolution mechanism for this setup.

There is no separate test framework, database, AI service, or external locator service in the current project.

## Features and locator design

### Field descriptors

`FieldDescriptor` stores the intended field's semantic name and HTML clues, including tag, ID, name, placeholder, type, and optional value. First Name additionally sets `RequireAssociatedLabel = true`. Gender supplies the expected value `Male`.

### Candidate collection and filters

`SelfHealingLocator` gathers elements matching the descriptor's tag from its search context. It skips hidden and disabled elements, filters by type when the descriptor specifies one, and filters by value when an expected value is supplied. Tag and type alone do not count as field identity.

### Weighted heuristic matching

The current scoring constants are:

| Evidence | Points |
|---|---:|
| Exact ID | 10 |
| Exact name | 14 |
| Exact placeholder | 8 |
| Exact associated-label text | 10 |
| Tag name | 2 |
| Type | 2 |

Text comparisons are case-insensitive. A candidate must have identity evidence and reach the minimum score of 10. If the top two candidate scores are less than 3 points apart, the locator throws an ambiguity exception. These are initial heuristic weights, not empirically calibrated accuracy scores.

### Associated-label validation for First Name

For each candidate, JavaScript reads the native `element.labels` collection and compares normalized label text with the descriptor's `FieldName`. It does not use arbitrary parent text or nearby sibling text.

The First Name descriptor requires an exact associated label. A label match is accepted only when the candidate has an owning form and the label's nearest form is the same DOM form element. This uses DOM element identity rather than comparing form IDs or names. When a label-matched candidate exists, unlabeled candidates are removed before score ranking.

This validation matters because the page contains duplicate form examples. If an input ID changes while the label's `for` remains unchanged, the intended input can lose its native association and the stale label may resolve to a different control. The locator must not treat that label as evidence for a control in another form. If no valid First Name label association remains, lookup is expected to fail safely rather than choose an unlabeled duplicate.

### Same-form scoping

`FindElementInSameForm` first locates the First Name anchor using its descriptor. It finds that element's nearest ancestor `<form>` and searches for the requested target within that form. It does not hardcode the form's current ID.

`FormAutomation` uses this method for both Gender and State. Gender is further restricted by its `type="radio"` and expected `value="Male"`. State is found within the same form; its option is selected by the configured value using an XPath relative to the located `<select>`.

### Verification and browser cleanup

First Name is verified by reading the selected input's current `value`. Gender is verified using Selenium's `Selected` property. State is verified by reading the selected `<select>` value. Chrome is closed in `finally` with `driver?.Quit()`.

## Project structure

```text
SelfHealingSelenium/
├── Automation/
│   └── FormAutomation.cs       # Form interactions and PASS/FAIL checks
├── Config/
│   └── Settings.cs             # URL override and test values
├── Locators/
│   ├── FieldDescriptors.cs     # Field descriptions and identity requirements
│   └── SelfHealingLocator.cs   # Candidate scoring, ambiguity, form scoping
├── Program.cs                  # Chrome lifecycle, navigation, output, cleanup
├── SelfHealingSelenium.csproj  # .NET target and Selenium package reference
├── .gitignore
└── README.md
```

The private local `PROJECT_NOTES.md` is intentionally excluded from this public project documentation and ignored by Git.

## Prerequisites

- .NET SDK 10.x. Check with `dotnet --version`.
- Google Chrome installed and available to the current user.
- Network access may be needed when Selenium Manager resolves a compatible ChromeDriver for the installed browser.

The project does not require a manually downloaded ChromeDriver path in its source configuration.

## Setup and run

Clone the repository and enter the project directory:

```bash
git clone https://github.com/sakshamiishra/SelfHealingSelenium.git
cd SelfHealingSelenium
```

Restore the NuGet dependencies and run the console application:

```bash
dotnet restore
dotnet run
```

`Program.cs` creates a new Chrome WebDriver session, navigates to `Settings.Url`, runs the form automation, prints the returned results, and quits the session in a `finally` block.

## Configuration

The default URL and test values are defined in `Config/Settings.cs`:

| Setting | Default |
|---|---|
| URL | `https://app.cloudqa.io/home/AutomationPracticeForm` |
| First Name | `Saksham` |
| Gender | `Male` |
| State | `India` |

Set `CLOUDQA_URL` to override the default URL.

macOS/Linux shell:

```bash
export CLOUDQA_URL="https://example.test/form"
dotnet run
```

PowerShell:

```powershell
$env:CLOUDQA_URL = "https://example.test/form"
dotnet run
```

Unset the variable to use the CloudQA default.

## Expected output

When the page loads, all locators find the intended fields, and each verification succeeds, the program prints:

```text
PASS: First Name is verified.
PASS: Gender is selected: Male.
PASS: State is selected: India.
```

This is the output format implemented by `Program.cs`; it is not a claim that the current implementation has been browser-tested for every page mutation listed below. If an exception stops automation, the catch block prints a `FAIL: Automation could not complete...` message and sets a non-zero exit code. If a check returns false, that check prints FAIL and the process exit code becomes 1.

## Testing and mutation scenarios

The following results are from manual browser testing. Tests 1–4 and the final normal automation run were performed with the latest associated-label validation implementation. Tests 5–6 passed before that guard was added and have not been retested against the latest implementation.

| Scenario | Result | Implementation version |
|---|---|---|
| First Name ID changed while label association was preserved | PASSED | Latest associated-label validation |
| First Name name changed | PASSED | Latest associated-label validation |
| First Name ID and name both changed while label association was preserved | PASSED | Latest associated-label validation |
| First Name ID and name both changed without preserving label association | PASSED: correctly rejected with `No visible, enabled candidate has an associated label matching 'First Name'.` | Latest associated-label validation |
| Form fields reordered | PASSED | Previously passed before latest associated-label guard; not retested afterward |
| Additional wrapper added around the input | PASSED | Previously passed before latest associated-label guard; not retested afterward |
| Normal automation after temporary debugging pauses were removed | PASSED | Latest associated-label validation |

The first four mutation scenarios and the final normal automation run were tested with the latest associated-label implementation. The reordering and wrapper scenarios are earlier results and should be retested after the guard change before treating them as verified for the current code.

## Limitations and failure conditions

- The locator is heuristic. It cannot guarantee recovery from arbitrary attribute, label, form, or structural changes.
- First Name requires native associated-label evidence in the same owning form. If the ID changes, update the label's `for` attribute as well, or lookup fails safely. If identifying evidence is removed, no recovery is claimed.
- If no candidate reaches the score threshold, the locator throws `NoSuchElementException`. If the top candidates are too close, it throws `InvalidOperationException` instead of choosing arbitrarily.
- The engine searches the current Selenium search context. It does not traverse iframes, open Shadow DOM roots, nested Shadow DOM, or iframe-in-Shadow-DOM structures. Those page examples are not supported by this implementation.
- The State `<select>` is located heuristically within the First Name form, but its option is still selected with a relative XPath using the configured option value.
- PASS checks validate the value or selection state of the element returned by the locator. The strict First Name label requirement helps identify that field, but the project does not independently compare the whole form against a saved page schema.

## Design decisions and future improvements

- Keep descriptors separate from search logic so field intent can be reviewed independently from candidate evaluation.
- Keep scoring deterministic and explainable; use safe exceptions when evidence is absent or ambiguous.
- Use associated-label evidence and same-form context to avoid cross-form false positives without depending on a hardcoded form ID.
- Preserve a small console-app architecture with no additional frameworks or services.

Possible future work, subject to validation and review:

- Add focused automated tests using controlled DOM fixtures for score ranking, label association, duplicate forms, and failure cases.
- Add configurable descriptor evidence and weights only after measuring behavior against representative pages.
- Add iframe or Shadow DOM traversal as a separate, explicitly tested capability.
- Improve verification so a check can independently assert the selected field's semantic identity as well as its value.
