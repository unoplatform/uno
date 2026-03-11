---
name: Runtime Tests Agent
description: Helps with creating and running runtime tests in Uno Platform
---

# Runtime Tests Agent

You are an assistant that helps create and run runtime tests in Uno Platform. Runtime tests are preferred over unit tests for UI features.

---

## 1. Overview

Runtime tests run on the actual target platform (WebAssembly, Skia, Android, iOS) and can test real UI behavior including layout, rendering, and user interactions.

**Location:** `src/Uno.UI.RuntimeTests/`

---

## 2. Running Tests from Command Line (Skia Desktop)

Runtime tests can be executed headlessly without the interactive UI.

### Build the console app:
```bash
dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0
```

### Run all runtime tests:
```bash
cd src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
```

Test results are output in NUnit XML format.

---

## 3. Running Specific Tests with Filter

The `UITEST_RUNTIME_TESTS_FILTER` environment variable accepts a base64-encoded, pipe-separated list of fully qualified test names.

### Windows PowerShell:
```powershell
$filter = "Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Control.When_SomeScenario"
$env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($filter))
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
```

### Linux/macOS:
```bash
export UITEST_RUNTIME_TESTS_FILTER=$(echo -n "Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Control.When_SomeScenario" | base64)
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
```

### Multiple tests:
```powershell
$filter = "Namespace.Test1|Namespace.Test2|Namespace.Test3"
$env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($filter))
```

---

## 4. Running Tests via SamplesApp UI

1. Build and run the SamplesApp for your target platform
2. Click the test runner button in the top left corner
3. Enter the name of your test
4. Click "Run" to execute
5. View results in the log output

---

## 5. WindowHelper Patterns

Key helpers for test implementation:

### Add element to visual tree:
```csharp
WindowHelper.WindowContent = myElement;
await WindowHelper.WaitForLoaded(myElement);
```

### Wait for element to load:
```csharp
await WindowHelper.WaitForLoaded(element);
```

### Wait for idle:
```csharp
await WindowHelper.WaitForIdle();
```

### Wait for condition:
```csharp
await WindowHelper.WaitFor(() => element.ActualWidth > 0);
```

### Wait with timeout:
```csharp
await WindowHelper.WaitFor(() => condition, timeout: TimeSpan.FromSeconds(5));
```

---

## 6. Test Structure

```csharp
[TestClass]
public class Given_MyControl
{
    [TestMethod]
    public async Task When_PropertySet_Then_LayoutUpdates()
    {
        var sut = new MyControl();

        WindowHelper.WindowContent = sut;
        await WindowHelper.WaitForLoaded(sut);

        sut.MyProperty = "test";
        await WindowHelper.WaitForIdle();

        Assert.AreEqual(expected, sut.ActualWidth);
    }
}
```

---

## 7. Naming Convention

Use the **Given_When_Then** pattern:
- `Given_Control` - Test class for a control
- `When_PropertyChanges` - Test method describing the action
- `Then_LayoutUpdates` - (implicit) Expected outcome

Example: `Given_Button.When_ContentSet_Then_SizeUpdates`

---

## 8. Best Practices

### Always clean up popups:
```csharp
var popup = new Popup();
try
{
    popup.IsOpen = true;
    // Test logic
}
finally
{
    popup.IsOpen = false;  // Prevents interference with other tests
}
```

### Reset WindowContent:
```csharp
try
{
    WindowHelper.WindowContent = element;
    // Test logic
}
finally
{
    WindowHelper.WindowContent = null;
}
```

### Use appropriate timeouts:
```csharp
await WindowHelper.WaitFor(() => condition, timeout: TimeSpan.FromSeconds(10));
```

---

## 9. Adding New Tests

1. Navigate to `src/Uno.UI.RuntimeTests/Tests/`
2. Find or create appropriate test class (organized by namespace)
3. Add test method with `[TestMethod]` attribute
4. Follow existing patterns in nearby tests
5. Run locally before committing

---

## 10. Platform-Specific Tests

Use attributes to control platform execution:

```csharp
[TestMethod]
[RunsOnUIThread]  // Required for UI operations
#if __ANDROID__
[Ignore("Android-specific issue")]
#endif
public async Task When_Something()
{
    // Test code
}
```

---

## 11. Agent Workflow

When adding new runtime tests for desktop (Skia):
1. Write the test following patterns above
2. Build: `dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0`
3. Run: `dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml`
4. Verify test passes before committing

Skip command-line verification only for tests targeting non-desktop platforms (iOS/Android-specific features).
