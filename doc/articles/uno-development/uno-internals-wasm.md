---
uid: Uno.Contributing.Wasm
---

# How Uno works on WebAssembly

This article explores WebAssembly-specific details of Uno's internals, with a focus on information that's useful for contributors to Uno. For an overview of how Uno works on all platforms, see [this article](uno-internals-overview.md).

## What is WebAssembly actually?

> [WebAssembly is a new type of code](https://developer.mozilla.org/en-US/docs/WebAssembly) that can be run in modern web browsers — it is a low-level assembly-like language with a compact binary format that runs with near-native performance and provides languages such as C/C++ and Rust with a compilation target so that they can run on the web. It is also designed to run alongside JavaScript, allowing both to work together.

WebAssembly (Wasm) allows .NET code, and hence Uno, to run in the browser. It's supported by all major browsers, including mobile browser versions.

Wasm in the browser runs in the same security sandbox as JavaScript does, and has exactly the same capabilities and constraints. There's no means of accessing browser APIs, including the DOM, directly from Wasm. All communication to and from the DOM must be done by interop with JavaScript.

## Skia rendering in the browser

WebAssembly is supported through the Skia rendering backend. The previous WebAssembly target, where each XAML element mapped to a DOM element, was removed in Uno Platform 7.0. The UI is now drawn onto a canvas with the same Skia-based rendering pipeline as the other platforms.

The browser host lives in [`Uno.UI.Runtime.WebAssembly.Browser`](https://github.com/unoplatform/uno/tree/master/src/Uno.UI.Runtime.WebAssembly.Browser), with its TypeScript layer under `ts/Runtime`:

- `BrowserRenderer` presents the frames drawn by Skia.
- `SemanticElements` maintains the hidden DOM elements that expose the accessibility tree to screen readers.
- `BrowserNativeElementHostingExtension` embeds HTML elements alongside the Skia-rendered tree, see [Embedding Native Elements in Skia Apps](xref:Uno.Skia.Embedding.Native).
