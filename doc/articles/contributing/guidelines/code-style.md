---
uid: Uno.Contributing.CodeStyle
---

# Guidelines for Code style

Uno uses EditorConfig ([here's our configuration](https://github.com/unoplatform/uno/blob/master/.editorconfig)) to maintain consistent coding styles and settings in our codebase, such as indent style, tab width, end of line characters, encoding, and more. Most IDEs should respect the `EditorConfig` settings by default when applying formatting. We typically observe the [Microsoft C# Coding Conventions](https://learn.microsoft.com/dotnet/csharp/programming-guide/inside-a-program/coding-conventions) with one notable exception - Uno uses Tabs. If you install this [Visual Studio plugin](https://marketplace.visualstudio.com/items?itemName=mynkow.FormatdocumentonSave) it will automatically format your contributions upon file save.

## Refactoring

Pure refactoring for its own sake should generally be done in a separate, refactoring-only pull request, and you should generally open an issue to initiate discussion with the core team before you start such a refactoring, to determine if it's really appropriate. See [this blog post on Open Source Contribution Etiquette](https://tirania.org/blog/archive/2010/Dec-31.html) for some explanation of the reasons why. Consistently-observed conventions are essential to the long-term health of the codebase.

Within a bugfix or enhancement PR, refactoring should be restricted to the relevant files, and it should respect the conventions of existing Uno code.

## Patterns

This section describes some recurring patterns and practices you'll see in Uno code.

### Partial classes

[Partial class definitions](https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods) are used extensively in Uno. The two main use cases for partial classes are [platform-specific code](../../platform-specific-csharp.md) and [generated code](xref:Uno.Contributing.Overviewmd#generated-notimplemented-stubs).

However, in some cases where it makes sense, partial class files are also used for logical separation of code. If you're implementing a type that owns a lot of dependency properties, consider putting these in a separate partial, to avoid cluttering up the file where the actual business logic is with DP boilerplate. Another use case for a partial is a nested class with a large definition.

### Disposables

Uno uses lightweight `IDisposables` widely for robust lifetime management. The most commonly used types for this purpose are `SerialDisposable`, `CompositeDisposable`, `CancellationDisposable`, and `DisposableAction`.

If you've used the `Reactive Extensions` framework, these names [might be familiar](https://learn.microsoft.com/previous-versions/dotnet/reactive-extensions/hh229090(v=vs.103)), and in fact these disposables behave identically to their Rx equivalents. However, they've been transplanted into [Uno](../../../../src/Uno.Foundation/Uno.Core.Extensions/Uno.Core.Extensions.Disposables/Disposables), to avoid having to take a dependency on `System.Reactive`.

### Extension methods

[Extension methods](https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/extension-methods) are used throughout the Uno Platform codebase to add reusable functionality to existing types, particularly types coming from the Xamarin bindings. Extension methods should be defined in a dedicated class, with the naming convention `[TypeName]Extensions.cs`, where `TypeName` is the name of the type either being returned or passed as the `this` parameter.

A number of extensions to the standard .NET types already exists in [Uno.Foundation](../../../../src/Uno.Foundation/Uno.Core.Extensions). So, you should check those first to see if they do what you need.

When adding a new extension method class, it should typically be marked `internal`, to avoid naming clashes with existing consumer code.

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

## Integration Tests (UI Tests)

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
