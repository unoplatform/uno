# `WebView` (`WebView2`)

> Uno Platform supports two `WebView` controls - a legacy `WebView` and a modernized `WebView2` control. For new development, we strongly recommend `WebView2` as it will get further improvements in the future.

`WebView2` is currently supported on Windows, Android, iOS, and macOS.

## Basic usage

You can include the `WebView2` control anywhere in XAML:

```xaml
<WebView2 x:Name="MyWebView" Source="https://platform.uno/" />
```

To manipulate the control from C#, first ensure that you call its `EnsureCoreWebView2Async` method:

```csharp
await MyWebView.EnsureCoreWebView2Async();
```

Afterward, you can perform actions such as navigating to an HTML string:

```csharp
MyWebView.NavigateToString("<html><body><p>Hello world!</p></body></html>");
```

## Executing JavaScript

When a page is loaded inside the `WebView2` control, you can execute custom JavaScript code. To do this, call the `ExecuteScriptAsync` method:

```csharp
webView.NavigateToString("<div id='test' style='width: 10px; height: 10px; background-color: blue;'></div>");
// Renders a blue <div>

await webView.ExecuteScriptAsync("document.getElementById('test').style.backgroundColor = 'red';");
// The <div> is now red.
```

The method can also return a string result, with returned values being JSON-encoded:

```csharp
await webView.ExecuteScriptAsync("1 + 1"); // Returns a string containing 2
await webView.ExecuteScriptAsync($"(1 + 1).toString()"); // Returns a string containing "2"
await webView.ExecuteScriptAsync("eval({'test': 1})"); // Returns a string containing {"test":1}
```

## JavaScript to C# communication

`WebView2` enables sending web messages from JavaScript to C# on all supported targets. In your web page, include code that sends a message to the `WebView2` control if available. Since Uno Platform runs on multiple targets, you need to use the correct approach for each. We recommend creating a reusable function like the following:

```javascript
function postWebViewMessage(message){
    try{
        if (window.hasOwnProperty("chrome") && typeof chrome.webview !== undefined) {
            // Windows
            chrome.webview.postMessage(message);
        } else if (window.hasOwnProperty("unoWebView")) {
            // Android
            unoWebView.postMessage(JSON.stringify(message));
        } else if (window.hasOwnProperty("webkit") && typeof webkit.messageHandlers !== undefined) {
            // iOS and macOS
            webkit.messageHandlers.unoWebView.postMessage(JSON.stringify(message));
        }
    }
    catch (ex){
        alert("Error occurred: " + ex);
    }
}

// Usage:
postWebViewMessage("hello world");
postWebViewMessage({"some": ['values',"in","json",1]});
```

> **Note:** Make sure not to omit the `JSON.stringify` calls for Android, iOS, and macOS as seen in the snippet above, as they are crucial to transfer data correctly.

To receive the message in C#, subscribe to the `WebMessageReceived` event:

```csharp
webView.WebMessageReceived += (s, e) =>
{
    Debug.WriteLine(e.WebMessageAsJson);
};
```

The `WebMessageAsJson` property contains a JSON-encoded string of the data passed to `postWebViewMessage` above.

## Navigating to web content in the application package

To load local web content bundled with the application, you can use the `SetVirtualHostNameToFolderMapping` method. This allows you to set a virtual hostname that maps to a folder within the package, from which the web content will be loaded:

```csharp
await webView.EnsureCoreWebView2Async();
webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
    "UnoNativeAssets",
    "WebContent",
    CoreWebView2HostResourceAccessKind.Allow);
webView.CoreWebView2.Navigate("http://UnoNativeAssets/index.html");
```

This will navigate to the `index.html` file stored in the `WebContent` folder. This folder must be included in a platform-specific location on each platform:

- On Windows, it should be directly in the root of the `YourApp.Windows` project and all its contents should be set to `Content` build action
- On iOS, it should be inside the `Resources` folder and all its contents should be set to `BundleResource` build action
- On Android, it should be inside the `Assets` folder and all its contents should be set to `AndroidAsset` build action

To avoid duplication, you can put the files in a non-project-specific location and add them via linking, e.g.:

```xml
<BundleResource Include="..\LinkedFiles\WebContent\css\site.css" Link="iOS\Resources\WebContent\css\site.css" />
```

The web files can reference each other in a relative path fashion, for example, the following HTML file:

```html
<html>
<head>
    <script src="js/site.js" type="text/javascript"></script>
</head>
<body>
    ...
</body>
</html>
```

Is referencing a `site.js` file inside the `js` subfolder.
