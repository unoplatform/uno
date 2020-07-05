# Guidelines for Code style

Uno uses EditorConfig ([here's our configuration](https://github.com/unoplatform/uno/blob/master/.editorconfig)) to maintain consistent coding styles and settings in our codebase, such as indent style, tab width, end of line characters, encoding, and more. Most IDEs should respect the `EditorConfig` settings by default when applying formatting. We typically observe the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions) with one notable exception - Uno uses Tabs. If you install this [Visual Studio plugin](https://marketplace.visualstudio.com/items?itemName=mynkow.FormatdocumentonSave) it will automatically format your contributions upon file save.

## Refactoring

Pure refactoring for its own sake should generally be done in a separate, refactoring-only pull request, and you should generally open an issue to initiate discussion with the core team before you start such a refactoring, to determine if it's really appropriate. See [this blog post on Open Source Contribution Etiquette](https://tirania.org/blog/archive/2010/Dec-31.html) for some explanation of the reasons why. Consistently-observed conventions are essential to the longterm health of the codebase.

Within a bugfix or enhancement PR, refactoring should be restricted to the relevant files, and it should respect the conventions of existing Uno code.

## Conventions

### Braces

```csharp
if (condition)
{
    // do something
}
else
{
    // use braces even for single line conditions
}
```            

## Integration Tests

```csharp
[ActivePlatforms(Platform.Android, Platform.Browser, Platform.iOS)]
[TestFixture]
public class LocalSettings_Tests : SampleControlUITestBase
{
    [Test]
    [AutoRetry]
    public void ClearAddContainsRemove()
    {
        // Navigate to this x:Class control name
        Run("UITests.Shared.Windows_Storage_ApplicationData.LocalSettings");

        // Define elements that will be interacted with at the start of the test
        var containerName = _app.Marked("ContainerName");
        var clearButton = _app.Marked("ClearButton");
        var addButton = _app.Marked("AddButton");
        var containsButton = _app.Marked("ContainsButton");
        var removeButton = _app.Marked("RemoveButton");
        var output = _app.Marked("Output");

        // Specify what user interface element to wait on before starting test execution
        _app.WaitForElement(clearButton);

        // Take an initial screenshot
        TakeScreenshot("Initial State");

        // Assert initial state
        Assert.AreEqual("Local", containerName.GetDependencyPropertyValue("Text")?.ToString());
        Assert.AreEqual(string.Empty, output.GetDependencyPropertyValue("Text")?.ToString());

        {
            _app.Tap(clearButton);
            _app.WaitForDependencyPropertyValue(output, "Text", 0);
            TakeScreenshot("Clear Button");
        }

        {
            _app.Tap(addButton);
           _app.WaitForDependencyPropertyValue(output, "Text", 1);
            TakeScreenshot("Add Button");
        }

        {
            _app.Tap(containsButton);
            _app.WaitForDependencyPropertyValue(output, "Text", "True");
            TakeScreenshot("Contains Button");
        }

        {
            _app.Tap(removeButton);
            _app.WaitForDependencyPropertyValue(output, "Text", 0);
            TakeScreenshot("Remove Button");
        }
    }
}
```
