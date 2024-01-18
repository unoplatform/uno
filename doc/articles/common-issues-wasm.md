---
uid: Uno.UI.CommonIssues.Wasm
---

# Issues related to WebAssembly projects

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
