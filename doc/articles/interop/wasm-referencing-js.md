---
uid: Uno.Interop.ReferenceJavaScript
---

# How to Reference JavaScript Files in WebAssembly Apps

When building Uno Platform WebAssembly applications, you often need to integrate existing JavaScript libraries and scripts. This guide explains the different methods available to reference and load JavaScript files in your WebAssembly apps.

## Overview

Uno Platform WebAssembly apps can reference JavaScript files in several ways:

1. **Embedded local scripts** - JavaScript files bundled with your app
2. **Static web assets** - Files served from the `wwwroot` folder
3. **External CDN scripts** - JavaScript libraries loaded from Content Delivery Networks
4. **Dynamic script loading** - Scripts loaded at runtime using JavaScript interop

## Method 1: Embedded Local Scripts (Recommended)

The most common and recommended approach is to embed JavaScript files in your project using the `WasmScripts` folder. These scripts are automatically loaded by the Uno Bootstrapper when the page loads.

### Using Uno.Sdk (Recommended)

If your project uses the [Uno.Sdk](xref:Uno.Features.Uno.Sdk), the process is straightforward:

1. Create a folder structure: `Platforms/WebAssembly/WasmScripts` in your app project
2. Place your JavaScript files in this folder (e.g., `mylib.js`)
3. The Uno.Sdk will automatically configure these files as embedded resources

**Example project structure:**

```
MyApp/
├── Platforms/
│   └── WebAssembly/
│       └── WasmScripts/
│           ├── AppManifest.js
│           └── mylib.js
└── MyApp.csproj
```

### Without Uno.Sdk

If you're not using Uno.Sdk, you need to manually mark the files as embedded resources in your `.csproj` file:

1. Create the `WasmScripts` folder as described above
2. Add the files to your project
3. Edit your `.csproj` file and add:

```xml
<ItemGroup>
  <EmbeddedResource Include="Platforms\WebAssembly\WasmScripts\*.js" />
</ItemGroup>
```

Or specify individual files:

```xml
<ItemGroup>
  <EmbeddedResource Include="Platforms\WebAssembly\WasmScripts\mylib.js" />
  <EmbeddedResource Include="Platforms\WebAssembly\WasmScripts\helper.js" />
</ItemGroup>
```

### Script Loading Order

Scripts in the `WasmScripts` folder are loaded automatically during the application bootstrap phase, before your C# code runs. If you need to control the loading order, consider:

- **Naming convention**: Scripts are typically loaded in alphabetical order
- **Dependencies**: If `scriptB.js` depends on `scriptA.js`, ensure scriptA loads first by naming or using a single combined file
- **Dynamic loading**: Use the dynamic loading approach (Method 4) if you need precise control

## Method 2: Static Web Assets (wwwroot)

The `wwwroot` folder works similarly to ASP.NET Core projects. Files placed here are served as static content but are **not** automatically loaded.

### When to Use wwwroot

Use the `wwwroot` folder when:

- You want to load scripts conditionally or on-demand
- You have large JavaScript files that shouldn't slow down initial page load
- You're serving other static content (images, data files, etc.) alongside scripts

### Setup

1. Create a `wwwroot` folder in your app project (usually at `Platforms/WebAssembly/wwwroot`)
2. Place your JavaScript files there (e.g., `wwwroot/js/mylib.js`)
3. Load the script manually using JavaScript interop

**Example project structure:**

```
MyApp/
├── Platforms/
│   └── WebAssembly/
│       └── wwwroot/
│           └── js/
│               └── mylib.js
└── MyApp.csproj
```

### Loading Scripts from wwwroot

To load a script from wwwroot at runtime, use JavaScript interop:

```csharp
using System.Runtime.InteropServices.JavaScript;

public partial class ScriptLoader
{
    [JSImport("globalThis.eval")]
    internal static partial string Eval(string code);

    public static void LoadScript(string scriptPath)
    {
        var loadScriptCode = $$"""
            (function() {
                const script = document.createElement('script');
                script.src = '{{scriptPath}}';
                script.async = true;
                document.head.appendChild(script);
            })();
            """;
        
        Eval(loadScriptCode);
    }
}
```

Then call it from your code:

```csharp
ScriptLoader.LoadScript("/js/mylib.js");
```

## Method 3: External CDN Scripts

Loading scripts from a CDN is useful for popular libraries like jQuery, Bootstrap, or other widely-used frameworks.

### Advantages of CDN

- Reduced app bundle size
- Potential browser caching benefits
- Always up-to-date (if not version-pinned)

### Disadvantages of CDN

- Requires internet connectivity
- Potential version changes that could break your app
- Third-party dependency on CDN availability

### Loading CDN Scripts

You have several options for loading scripts from a CDN:

#### Option A: Add to index.html

For the Native WebAssembly renderer, modify the `index.html` file in your project:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>My App</title>
    <!-- Add your CDN scripts here -->
    <script src="https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js"></script>
</head>
<body>
    <!-- App content -->
</body>
</html>
```

#### Option B: Dynamic Loading via JavaScript

Create a script in `WasmScripts` that loads the CDN resource:

```javascript
// File: Platforms/WebAssembly/WasmScripts/cdn-loader.js
(function() {
    const script = document.createElement('script');
    script.src = 'https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js';
    script.async = false; // Load synchronously if order matters
    document.head.appendChild(script);
})();
```

#### Option C: Using AMD/RequireJS (for Uno Bootstrapper)

The Uno Bootstrapper includes AMD module support. You can use `require()` to load scripts:

```csharp
var javascript = @"require(['https://cdn.jsdelivr.net/npm/lodash@4.17.21/lodash.min.js'], 
    function(_) {
        console.log('Lodash loaded:', _.VERSION);
    }
);";

WebAssemblyRuntime.InvokeJS(javascript);
```

## Method 4: Dynamic Script Loading at Runtime

For maximum control, you can load scripts dynamically at runtime from your C# code.

### Using JSImport/JSExport

With .NET 7+ and the new JSImport/JSExport APIs:

```csharp
using System.Runtime.InteropServices.JavaScript;

public partial class DynamicScriptLoader
{
    [JSImport("globalThis.eval")]
    internal static partial string Eval(string code);

    public static Task<bool> LoadScriptAsync(string url)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        var loadScriptCode = $$"""
            (function() {
                const script = document.createElement('script');
                script.src = '{{url}}';
                script.onload = () => globalThis.scriptLoaded = true;
                script.onerror = () => globalThis.scriptLoaded = false;
                document.head.appendChild(script);
            })();
            """;
        
        Eval(loadScriptCode);
        
        // Poll for completion (in production, use a proper callback mechanism)
        return Task.Run(async () =>
        {
            for (int i = 0; i < 50; i++)
            {
                await Task.Delay(100);
                var result = Eval("globalThis.scriptLoaded");
                if (result == "true") return true;
                if (result == "false") return false;
            }
            return false;
        });
    }
}
```

### Using Legacy WebAssemblyRuntime (Uno.Foundation)

For older projects or when targeting Uno.Foundation APIs:

```csharp
using Uno.Foundation;

public static class ScriptLoader
{
    public static void LoadScript(string url)
    {
        var javascript = $@"
            (function() {{
                const script = document.createElement('script');
                script.src = '{url}';
                document.head.appendChild(script);
            }})();
        ";
        
        WebAssemblyRuntime.InvokeJS(javascript);
    }
}
```

## CSS Files

While this guide focuses on JavaScript, CSS files work similarly:

1. **Embedded CSS**: Place files in `Platforms/WebAssembly/WasmCSS` folder
   - Automatically loaded and added to the HTML `<head>`
   - Must be marked as `EmbeddedResource` if not using Uno.Sdk

2. **Static CSS**: Place files in `wwwroot` folder
   - Load manually using `<link>` tag injection

```javascript
// Loading CSS from JavaScript
const link = document.createElement('link');
link.rel = 'stylesheet';
link.href = 'https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css';
document.head.appendChild(link);
```

## Best Practices

### 1. Choose the Right Method

- **Use WasmScripts** for essential scripts that should load with the app
- **Use wwwroot** for on-demand or conditional scripts
- **Use CDN** for popular libraries to reduce bundle size
- **Use dynamic loading** when you need runtime control

### 2. Performance Considerations

- Minimize the number of script files to reduce HTTP requests
- Consider bundling and minifying scripts
- Use async/defer attributes when appropriate
- Lazy-load non-critical scripts

### 3. Version Pinning

When using CDN scripts, always pin to specific versions:

```html
<!-- Good: Version pinned -->
<script src="https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js"></script>

<!-- Bad: Latest version (can break unexpectedly) -->
<script src="https://cdn.jsdelivr.net/npm/jquery@latest/dist/jquery.min.js"></script>
```

### 4. Fallback Strategy

For critical CDN dependencies, implement a fallback:

```javascript
// Load from CDN with local fallback
(function() {
    const script = document.createElement('script');
    script.src = 'https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js';
    script.onerror = function() {
        // CDN failed, load from local
        const fallback = document.createElement('script');
        fallback.src = '/js/jquery.min.js';
        document.head.appendChild(fallback);
    };
    document.head.appendChild(script);
})();
```

### 5. Security Considerations

- Use Subresource Integrity (SRI) when loading from CDN
- Validate script sources
- Be cautious with `eval()` and dynamic script execution
- Review third-party scripts for security vulnerabilities

```html
<!-- Using SRI for security -->
<script src="https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js"
        integrity="sha256-/xUj+3OJU5yExlq6GSYGSHk7tPXikynS7ogEvDej/m4="
        crossorigin="anonymous"></script>
```

## Examples

### Example 1: Loading a Syntax Highlighter

```xml
<!-- In your project: Platforms/WebAssembly/WasmScripts/prism.js -->
<!-- Downloaded from https://prismjs.com/ -->
```

```xml
<ItemGroup>
  <EmbeddedResource Include="Platforms\WebAssembly\WasmScripts\prism.js" />
  <EmbeddedResource Include="Platforms\WebAssembly\WasmCSS\prism.css" />
</ItemGroup>
```

Now you can use PrismJS in your app. See [Part 2: Embedding JavaScript Components](xref:Uno.Interop.WasmJavaScript2) for a complete example.

### Example 2: Loading Chart.js from CDN

```javascript
// File: Platforms/WebAssembly/WasmScripts/load-chartjs.js
(function() {
    const script = document.createElement('script');
    script.src = 'https://cdn.jsdelivr.net/npm/chart.js@3.9.1/dist/chart.min.js';
    script.async = true;
    document.head.appendChild(script);
})();
```

### Example 3: Conditional Script Loading

```csharp
public class MapControl : Control
{
    protected override void OnLoaded()
    {
        base.OnLoaded();
        
        if (OperatingSystem.IsBrowser())
        {
            // Load Google Maps API only when needed
            LoadGoogleMapsScript();
        }
    }
    
    private void LoadGoogleMapsScript()
    {
        var apiKey = "YOUR_API_KEY";
        var script = $@"
            (function() {{
                const script = document.createElement('script');
                script.src = 'https://maps.googleapis.com/maps/api/js?key={apiKey}';
                script.async = true;
                document.head.appendChild(script);
            }})();
        ";
        
        WebAssemblyRuntime.InvokeJS(script);
    }
}
```

## Troubleshooting

### Scripts Not Loading

1. **Check file build action**: Ensure scripts in `WasmScripts` are marked as `EmbeddedResource`
2. **Verify path**: Check that the folder structure is exactly `Platforms/WebAssembly/WasmScripts`
3. **Browser console**: Open browser DevTools and check for 404 errors or JavaScript errors
4. **Build output**: Check if scripts are copied to the output directory

### Script Load Order Issues

1. Use synchronous loading: Set `script.async = false`
2. Chain script loading using callbacks
3. Use a single concatenated script file
4. Implement proper dependency management

### CORS Issues with CDN

If you encounter CORS errors:

1. Verify the CDN supports CORS
2. Check the `crossorigin` attribute
3. Consider downloading and hosting the script locally

## See Also

- [Embedding JavaScript Components - Part 1](xref:Uno.Interop.WasmJavaScript1)
- [Embedding JavaScript Components - Part 2](xref:Uno.Interop.WasmJavaScript2)
- [Embedding JavaScript Components - Part 3](xref:Uno.Interop.WasmJavaScript3)
- [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk)
- [AppManifest for WebAssembly](xref:Uno.Development.WasmAppManifest)
- Uno Wasm Bootstrapper Documentation (see Tooling section)
