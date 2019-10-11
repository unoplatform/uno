# Philosophy of Uno

This document outlines the philosophy of Uno. It guides the development of past and future major architectural decisions.

## Leverage existing tools

We stand on the shoulders of giants, Microsoft's tooling is a treat to work with:

- [Edit and Continue](https://docs.microsoft.com/en-us/visualstudio/debugger/edit-and-continue)
- [Live Visual Tree](https://docs.microsoft.com/en-us/visualstudio/debugger/inspect-xaml-properties-while-debugging).
- [XAML Hot Reload](https://docs.microsoft.com/en-us/visualstudio/debugger/xaml-hot-reload?view=vs-2019)

The promise of the Uno Platform is to enable building your app with those tools and then deploying it to iOS, Android, and the web via WebAssembly. 

## Create rich, responsive UIs

Bland apps don't quite cut it these days. Strong support for animations, templating, and custom visual effects is a must. When phones come in all sizes and manufacturers are [gouging holes out of the screen area](https://www.cnet.com/pictures/phones-with-notches/), your app's layout had better been responsive. 

## Let views do views

Separation of model, view and presentation keeps your code loosely coupled and easy to maintain. Features like data binding and attached properties let you write clean, elegant MVVM-style code. 

## Native intercompatibility (leave an escape hatch)

100% code reuse is the ideal, but it must also be easy to access functionality specific to a single platform, or to incorporate native third-party libraries., and the Uno Platform must make as much as possible to not stand in the way.

## Performance is a feature

The slow antelope gets eaten, and the slow app gets 1-star ratings. We've done a lot of optimisation on the basis of profiling in real-world use cases, and we'll continue to do so. 
