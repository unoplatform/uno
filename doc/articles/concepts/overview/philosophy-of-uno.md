---
uid: Uno.Overview.Philosophy
---

# Philosophy of Uno Platform

This document outlines the philosophy of Uno Platform. It guides the development of past and future major architectural decisions.

## Leverage existing tools

We stand on the shoulders of giants, Microsoft's tooling is a treat to work with:

- [Edit and Continue](https://learn.microsoft.com/visualstudio/debugger/edit-and-continue)
- [Live Visual Tree](https://learn.microsoft.com/visualstudio/debugger/inspect-xaml-properties-while-debugging)
- [XAML Hot Reload](https://learn.microsoft.com/visualstudio/debugger/xaml-hot-reload?view=vs-2019)

The promise of the Uno Platform is to enable building your app with those tools and then deploying it to WebAssembly, iOS, Android, macOS, and Linux.

## Create pixel-perfect, rich, and responsive UIs

Developers shouldn't sacrifice application looks for personal productivity. You can have both. Strong support for animations, templating, and custom visual effects is a must. When phones come in all sizes and manufacturers are [gouging holes out of the screen area](https://www.cnet.com/pictures/phones-with-notches/), your application's layout better be responsive and pixel-perfect or today's users, be it in corporate or private setting will not use it.

## Let views do views

Separation of model, view, and presentation keeps your code loosely coupled and easy to maintain. Features like data binding and attached properties let you write clean, elegant MVVM-style code.

## Native inter-compatibility (leave an escape hatch)

100% code reuse is the ideal, but it must also be easy to access functionality specific to a single platform in case it is a must. Also, a platform must make it easy to incorporate native third-party libraries. Uno Platform is architected to make this possible.

## Performance is a feature

The slow antelope gets eaten, and the slow app gets 1-star ratings. We've done a lot of optimization on the basis of profiling in real-world use cases, and we'll continue to do so.
