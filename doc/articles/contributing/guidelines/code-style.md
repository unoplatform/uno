# Guidelines for Code style

There are a mixture of patterns within the codebase of Uno and in the interest to ensure the correct patterns proliferate this page serves as the projects north star. When you are resolving a bug or feature please refactor the surrounding code thus [leave the code cleaner than you found it](https://www.matheus.ro/2017/12/11/clean-code-boy-scout-rule/) whilst keeping the changes minimal:

> Some developers, when faced with fixing, or adding a feature to an open source project are under the mistaken impression that the first step before any fixing takes place, or before adding a new feature takes place is to make the code "easier for them" to work on.
>
> "Easier for them" usually is a combination of renaming methods, fields, properties, locals; Refactoring of methods, classes; Gratuitous split of code in different files, or merging of code into a single file; Reorganization by alphabetical order, or functional order, or grouping functions closer to each other, or having helper methods first, or helper methods last. Changing indentation, aligning variables, or parameters or dozen other smaller changes.
>
> This is *not how you contribute to an open source project*.
>
> When you contribute fixes or new features to an open source project you should use the existing coding style, the existing coding patterns and stick by the active maintainer's choice for his code organization.
>
> The maintainer is in for the long-haul, and has been working on this code for longer than you have. Chances are, he will keep doing this even after you have long moved into your next project.
>
> Sending a maintainer a patch, or a pull request that consists of your "fix" mixed with a dozen renames, refactoring changes, variable renames, method renames, file splitting, layout changing code is not really a contribution, it is home work.
>
> The maintainer now has to look at your mess of a patch and extract the actual improvement, wasting precious time that could have gone to something else. This sometimes negates the effort of your "contribution".
>
> *Open Source Contribution Etiquette by Miguel de Icaza - https://tirania.org/blog/archive/2010/Dec-31.html*


Uno uses EditorConfig ([here's our configuration](https://github.com/unoplatform/uno/blob/master/.editorconfig)) to maintain consistent coding styles and settings in our codebase, such as indent style, tab width, end of line characters, encoding, and more. We typically observe the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions) with one notable exception - Uno uses Tabs. If you install this [Visual Studio plugin](https://marketplace.visualstudio.com/items?itemName=mynkow.FormatdocumentonSave) it will automatically format your contributions upon file save.

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

## Runtime Tests

## Performance Tests

## Windows.UI Controls
