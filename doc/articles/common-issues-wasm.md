---
uid: Uno.UI.CommonIssues.Wasm
---

# Issues related to WebAssembly projects

## WebAssembly: DllImport unable to load library 'libSkiaSharp'

If you're getting the following message in the browser debugger:

```text
[MONO] DllImport unable to load library 'libSkiaSharp'.
```

Here are a few ways to fix this:

- Make sure that you've run [`uno-check`](xref:UnoCheck.UsingUnoCheck) and that the `dotnet workload list` shows the `wasm-tools` workload. If you're using Visual Studio, make sure to restart it if you've installed workloads.
- If you've run `dotnet publish`, make sure to use the `bin\Release\netX.0-browserwasm\publish\wwwroot` folder to serve your app. Make sure to visit [our publishing docs](xref:uno.publishing.overview) for more information.

## Mixed Content, this request has been blocked

When running a webassembly app under WebAssembly with HTTPS the following error may happen:

```text
Mixed Content: The page at 'https://localhost:5002' was loaded over HTTPS, but attempted to 
connect to the insecure WebSocket endpoint 'ws://XXXX:59867/rc'. This request has been blocked;
this endpoint must be available over WSS.
```

In order to fix this issue, you may need to apply the [HTTPS fixes](xref:Uno.Guides.UsingTheServerProject#adjusting-for-https) mentioned in the server project documentation.

## WebAssembly: Access to fetch at 'https://XXXX' from origin 'http://XXXX' has been blocked by CORS policy

This is a security restriction from the JavaScript `fetch` API, where the endpoint you're calling needs to provide [CORS headers](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS) to work properly.

If you control the API, you'll need to use the features from your framework to enable CORS, and if you don't you'll need to ask the maintainers of the endpoint to enable CORS.

To test if CORS is really the issue, you can use [CORS Anywhere](https://cors-anywhere.herokuapp.com/) to proxy the queries.

### Build error `Failed to generate AOT layout`

When building for WebAssembly with AOT mode enabled, the following error may appear:

```console
Failed to generate AOT layout (More details are available in diagnostics mode or using the MSBuild /bl switch)
```

To troubleshoot this error, you can change the text output log level:

- Go to **Tools**, **Options**, **Projects and Solution**, then **Build and Run**
- Set **MSBuild project build output verbosity** to **Normal** or **Detailed**
- Build your project again and take a look at the additional output next to the `Failed to generate AOT layout` error

You can get additional build [troubleshooting information here](uno-builds-troubleshooting.md).
