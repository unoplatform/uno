---
uid: Uno.Interop.WasmJavaScript1
---

# Embedding Existing JavaScript Components Into Uno-WASM - Part 1

## HTML5 is a Rich and Powerful Platform

Uno Platform fully embraces HTML5 as its display backend when targeting WebAssembly, for both Native and Skia renderers. As a result, it is possible to integrate with almost any existing JavaScript library to extend the behavior of an app.

## Embedding assets

In the HTML world, everything running in the browser is assets that must be downloaded from a server. To integrate existing JavaScript frameworks, they can be either downloaded from another location on the Internet (usually from a CDN service) or embedded and deployed with the app.

The Uno Bootstrapper can automatically embed any asset and deploy it with the app. Some of them (CSS & JavaScript) can also be loaded with the app. Here's how to declare them in a *Uno Wasm* project:

1. **JavaScript files** should be in the `Platforms/WebAssembly/WasmScripts` folder. They will be copied to the output folder and loaded automatically by the bootstrapper when the page loads.

2. **CSS Style files** should be in the `Platforms/WebAssembly/WasmCSS` folder. They will be copied to the output folder and referenced in the *HTML head* of the application.

3. **Asset files** can be placed in the `Assets` folder. These files will be copied to the output folder and will preserve the same relative path to the `Assets` folder.

4. Alternatively, **any kind of asset file** can be placed directly in the `wwwroot` folder as with any standard ASP.NET Core project. They will be deployed with the app, but the application code is responsible for fetching and using them.

   > **Is it an ASP.NET Core "web" project?**
   > No, but it shares a common structure. Some of the deployment features, like the `wwwroot` folder, and the Visual Studio integration for running/debugging are reused in a similar way to an ASP.NET Core project. The C# code put in the project will run in the browser, using the .NET runtime. There is no need for a server side component in Uno-Wasm projects.

## Embedding Native Elements

Embedding native JavaScript elements is done through the `Uno.UI.NativeElementHosting.BrowserHtmlElement` class, which serves as an entry point to interact with your native element.

> [!IMPORTANT]
> The `BrowserHtmlElement` class is available on all target frameworks, eliminating the need for `#if` condition code, but it is only usable on WebAssembly. You'll need to guard its use by validating the platform with the `OperatingSystem` class.

```csharp
using Uno.UI.NativeElementHosting.BrowserHtmlElement;

public sealed partial class MyControlHost : ContentControl
{
  private BrowserHtmlElement? _element;

  public MyControlHost()
  {
    if (OperatingSystem.IsBrowser())
    {
      _element = BrowserHtmlElement.CreateHtmlElement("div");
      Content = _element;
    }
    else
    {
      Content = "This control only supported on WebAssembly on the Browser";
    }
  }
}
```

This way, your native element is hosted inside a content control of your choosing. You can replace it with any available HTML tag.

Once created, it is possible to interact directly with this element by calling `BrowserHtmlElement` methods.

Here is a list of helper methods used to facilitate the integration with the HTML DOM:

* The method `element.SetCssStyle()` can be used to set a CSS Style on the HTML element. Example:

  ```csharp
  // Setting only one CSS style
  _element.SetCssStyle("text-shadow", "2px 2px red");

  // Setting many CSS styles at once using C# tuples
  _element.SetCssStyle(("text-shadow", "2px 2px blue"), ("color", "var(--app-fg-color1)"));
  ```

* The `element.ClearCssStyle()` method can be used to set CSS styles to their default values. Example:

  ```csharp
  // Reset text-shadow style to its default value
  _element.ClearCssStyle("text-shadow");

  // Reset both text-shadow and color to their default values
  _element.ClearCssStyle("text-shadow", "color");
  ```

* The `element.SetHtmlAttribute()` and `element.ClearHtmlAttribute()` methods can be used to set HTML attributes on the element:

  ```csharp
  // Set the "href" attribute of an <a> element
  _element.SetHtmlAttribute("href", "#section2");

  // Set many attributes at once (less interop)
  _element.SetHtmlAttribute(("target", "_blank"), ("referrerpolicy", "no-referrer"));

  // Remove attribute from DOM element
  _element.ClearHtmlAttribute("href");

  // Get the value of an attribute of a DOM element
  var href = _element.GetHtmlAttribute("href");
  ```

* The `element.SetCssClass()` and `element.UnsetCssClass()` methods can be used to add or remove CSS classes to the HTML Element:

  ```csharp
  // Add the class to element
  _element.SetCssClass("warning");

  // Add many classes at once (less interop)
  _element.SetCssClass("warning", "level2");

  // Remove class from element
  _element.UnsetCssClass("paused");

  // You can also set one class from a list of possible values.
  // Like a radio-button, like non-selected values will be unset
  var allClasses = new [] { "Small", "Medium", "Large"};
  _element.SetCssClass(allClasses, 2); // set to "Large"
  ```

* The `element.SetHtmlContent()` method can be used to set arbitrary HTML content as child of the control.

  ```csharp
  _element.SetHtmlContent("<h2>Welcome to Uno Platform!</h2>");
  ```

* Finally, it is possible to make calls from and to JavaScript code by using [JSImport/JSExport](xref:Uno.Wasm.Bootstrap.JSInterop). The javascript code is directly executed in the context of the browser, giving the ability to perform anything that JavaScript can do. See next section for more details.

## Raising custom Javascript events

It's also possible to have Javscript components raise events to be handled by C# code.

From your Javascript or TypeScript code, you can raise events:

```javascript
  // Generate a custom generic event from JavaScript/Typescript
  htmlElement.dispatchEvent(new Event("simpleEvent"));

  // Generate a custom event with a string payload
  const payload = "this is the payload of the event";
  htmlElement.dispatchEvent(new CustomEvent("stringEvent", { detail: payload }));

  // Generate a custom event with a complex payload
  const payload = { property:"value", property2: 1234 };
  htmlElement.dispatchEvent(new CustomEvent("complexEvent", { detail: payload }));
```

Then from your C# code, add the following:

```csharp
  protected override void OnLoaded()
  {
      // Note: following extensions are in the namespace "Uno.Extensions"
      this.RegisterHtmlEventHandler("simpleEvent", OnSimpleEvent);
      this.RegisterHtmlEventHandler("stringEvent", OnStringEvent);
      this.RegisterHtmlEventHandler("complexEvent", OnComplexEvent);
  }

  private void OnSimpleEvent(object sender, JSObject args)
  {
      // You can react on "simpleEvent" here
  }

  private void OnStringEvent(object sender, JSObject args)
  {
      // You can react on "stringEvent" here
      var detail = args.GetPropertyAsString("detail");
  }

  private void OnComplexEvent(object sender, JSObject args)
  {
      // You can react on "complexEvent" here
      var detail = args.GetPropertyAsJSObject("detail");
      var property = detail?.GetPropertyAsString("property");
      var property2 = detail?.GetPropertyAsInt32("property2");
  }
```

## 🔬 Going further

* [Continue with Part 2](xref:Uno.Interop.WasmJavaScript2) - an integration of a syntax highlighter component.
* [Continue with Part 3](xref:Uno.Interop.WasmJavaScript3) - an integration of a more complex library with callbacks to application.
* Read the [Uno Wasm Bootstrapper](xref:UnoWasmBootstrap.Overview) documentation.
