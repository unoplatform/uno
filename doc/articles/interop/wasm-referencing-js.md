---
uid: Uno.Interop.ReferenceJavaScript
---

# How to Reference JavaScript Files in WebAssembly Apps

When building Uno Platform WebAssembly applications, you often need to integrate existing JavaScript libraries and scripts. This guide provides additional methods and best practices for loading JavaScript files beyond the basics covered in [Part 1: Embedding JavaScript Components](xref:Uno.Interop.WasmJavaScript1).

> [!NOTE]
> For the fundamentals of embedding JavaScript files using `WasmScripts`, `WasmCSS`, and `wwwroot` folders, see [Part 1: Embedding JavaScript Components](xref:Uno.Interop.WasmJavaScript1).

## Loading JavaScript from CDN

Loading scripts from a CDN is useful for popular libraries to reduce bundle size and leverage browser caching.

### Advantages and Disadvantages

**Advantages:**
- Reduced app bundle size
- Potential browser caching benefits
- Always up-to-date (if not version-pinned)

**Disadvantages:**
- Requires internet connectivity
- Potential version changes that could break your app
- Third-party dependency on CDN availability

### Option A: Add to index.html

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

### Option B: Dynamic Loading via JavaScript

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

### Option C: Using AMD/RequireJS (for Uno Bootstrapper)

The Uno Bootstrapper includes AMD module support. You can use `require()` to load scripts:

```csharp
var javascript = @"require(['https://cdn.jsdelivr.net/npm/lodash@4.17.21/lodash.min.js'], 
    function(_) {
        console.log('Lodash loaded:', _.VERSION);
    }
);";

WebAssemblyRuntime.InvokeJS(javascript);
```

## Dynamic Script Loading at Runtime

For maximum control over when and how scripts are loaded, you can load them dynamically at runtime from your C# code using `WebAssemblyRuntime.InvokeJS`:

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
                script.async = true;
                document.head.appendChild(script);
            }})();
        ";
        
        WebAssemblyRuntime.InvokeJS(javascript);
    }
}
```

### Conditional Loading Example

```csharp
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation;

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

## Best Practices

### Version Pinning

When using CDN scripts, always pin to specific versions to avoid unexpected breaking changes:

```html
<!-- Good: Version pinned -->
<script src="https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js"></script>

<!-- Bad: Latest version (can break unexpectedly) -->
<script src="https://cdn.jsdelivr.net/npm/jquery@latest/dist/jquery.min.js"></script>
```

### Fallback Strategy

For critical CDN dependencies, implement a fallback to a local copy:

```javascript
// Load from CDN with local fallback
(function() {
    const script = document.createElement('script');
    script.src = 'https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js';
    script.onload = function() {
        console.log('jQuery loaded from CDN');
    };
    script.onerror = function() {
        // CDN failed, load from local
        const fallback = document.createElement('script');
        fallback.src = '/js/jquery.min.js';
        document.head.appendChild(fallback);
    };
    document.head.appendChild(script);
})();
```

### Security Considerations

- Use Subresource Integrity (SRI) when loading from CDN to ensure the script hasn't been tampered with
- Validate script sources
- Review third-party scripts for security vulnerabilities

```html
<!-- Using SRI for security -->
<script src="https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js"
        integrity="sha256-/xUj+3OJU5yExlq6GSYGSHk7tPXikynS7ogEvDej/m4="
        crossorigin="anonymous"></script>
```

## Troubleshooting

### Scripts Not Loading

1. **Check file build action**: Ensure scripts in `WasmScripts` are marked as `EmbeddedResource` (if not using Uno.Sdk)
2. **Verify path**: Check that the folder structure is exactly `Platforms/WebAssembly/WasmScripts`
3. **Browser console**: Open browser DevTools and check for 404 errors or JavaScript errors
4. **Build output**: Verify scripts are copied to the output directory

### Script Load Order Issues

1. Use synchronous loading: Set `script.async = false`
2. Chain script loading using onload callbacks
3. Use a single concatenated script file

### CORS Issues with CDN

If you encounter CORS errors when loading from CDN:

1. Verify the CDN supports CORS
2. Check the `crossorigin` attribute
3. Consider downloading and hosting the script locally

## See Also

- [Embedding JavaScript Components - Part 1](xref:Uno.Interop.WasmJavaScript1)
- [Embedding JavaScript Components - Part 2](xref:Uno.Interop.WasmJavaScript2)
- [Embedding JavaScript Components - Part 3](xref:Uno.Interop.WasmJavaScript3)
- [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk)
- [AppManifest for WebAssembly](xref:Uno.Development.WasmAppManifest)
