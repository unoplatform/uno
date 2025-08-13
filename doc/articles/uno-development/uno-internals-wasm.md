---
uid: Uno.Contributing.Wasm
---

# How Uno works on WebAssembly Native

This article explores WebAssembly-specific details of Uno's internals, with a focus on information that's useful for contributors to Uno. For an overview of how Uno works on all platforms, see [this article](uno-internals-overview.md).

## What is WebAssembly actually?

> [WebAssembly is a new type of code](https://developer.mozilla.org/en-US/docs/WebAssembly) that can be run in modern web browsers — it is a low-level assembly-like language with a compact binary format that runs with near-native performance and provides languages such as C/C++ and Rust with a compilation target so that they can run on the web. It is also designed to run alongside JavaScript, allowing both to work together.

WebAssembly (Wasm) allows .NET code, and hence Uno, to run in the browser. It's supported by all major browsers, including mobile browser versions.

Wasm in the browser runs in the same security sandbox as JavaScript does, and has exactly the same capabilities and constraints. Note that for now there's no means of accessing browser APIs, including the DOM, directly from Wasm (though this is [expected to become available in the future](https://github.com/WebAssembly/interface-types/blob/master/proposals/interface-types/Explainer.md)). All communication to and from the DOM must be done by interop with JavaScript.

## UIElements map to DOM elements

There is for the most part a 1:1 mapping between managed XAML elements and DOM elements. The element tag associated with a XAML type is set by a `UIElement` constructor overload which takes the tag as a string, with "div" as the default.

## Typescript layer

Part of Uno for WASM is defined in Typescript (which transpiles to JavaScript), since going through JavaScript is currently the only way to manipulate the DOM and access other browser APIs from WebAssembly. The TypeScript layer is located in the Uno.UI.Wasm project. The most important class here is `WindowManager.ts`.

## JavaScript interop in Uno

Calls into JavaScript from C# generally access methods in the `WindowManagerInterop` .NET class, which calls methods on the `WindowManager` TypeScript class using the `WebAssemblyRuntime.InvokeJS()` method.

Callbacks into C# from JavaScript can be defined using the `mono_bind_static_method` wrapper. C# methods that will be called from JavaScript must be added to the [LinkerDefinition](https://github.com/unoplatform/uno/blob/master/src/Uno.UI/LinkerDefinition.Wasm.xml) file.
