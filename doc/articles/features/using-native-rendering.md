---
uid: uno.features.renderer.native
---

# The Native Renderer

The native renderer is our oldest backend, which uses the native components and APIs to render the UI.

Each supported platform (iOS, Android, and WebAssembly) has its own set of platform interactions, listed below, allowing for deep integration of native components into the visual tree. Each `UIElement` has a corresponding native element (`div` on WebAssembly, `UIView` on iOS, `ViewGroup` on Android).

This renderer uses the native input controls of the platforms, providing the best access to accessibility and IME features.

This renderer supports [integrating native views](xref:Uno.Development.NativeViews).

## Web (WebAssembly)

On WebAssembly, XAML elements are translated into HTML elements:

- Panels and layout containers are materialized as `<div>`
- Leaf controls like `TextBlock`, `Image` are materialized as semantic elements like `<p>`, `<img>`, etc.

This rendering integrates with CSS and DOM APIs, enabling UI behavior consistent with web standards.

## iOS

- All `FrameworkElement` types are backed by native `UIView` instances.
- Controls like `Image` implicitly create native `UIImageView` subviews.
- Native input, layout, and accessibility features are utilized.

## Android

- All `FrameworkElement` types inherit from native `ViewGroup`.
- Controls like `Image` wrap native `ImageView` instances.
- Leverages Android’s native rendering, accessibility, and gesture systems.
