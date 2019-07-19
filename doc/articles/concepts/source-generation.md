# Source Generation Overview

At compile time, there's two main things under the hood that Uno does:

- Parses XAML files and generates C# code to create the information needed to build your applications visual tree.
- Generation of dependency objects that are optimised for static type-checking where possible.


# Parsing of XAML files

tba

# Generation of DependencyObject's

Part of the power of Uno on Android and iOS is the ability to easily mix UWP view types with purely native views. This is possible because, in Uno, all views inherit from the native base view type.

- On Android this means [View](https://developer.android.com/reference/android/view/View).
- On iOS this means [UIView](https://developer.apple.com/documentation/uikit/uiview).

This however posed a challenge for reproducing UWP's inheritance hierarchy as `UIElement` is the primitive view type in UWP, which derives from the `DependencyObject` class which is the base class for anything that has `DependencyProperties`, that is, anything that supports databinding such as views, as well as some non-view framework types like [Transforms](https://docs.microsoft.com/en-us/windows/uwp/design/layout/transforms) and [Brushes](https://docs.microsoft.com/en-us/windows/uwp/design/style/brushes).

Since Uno can't change the design of the iOS or Android frameworks, Uno chose to make `DependencyObject` a [interface](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/interfaces/index) with a implementation that is automagically supplied by code generation. This design decision allows an Uno `FrameworkElement` to be a `UIView` and at the same time to be a `DependencyObject`. 

Most statically-typed languages, except C++, don't permit multiple base classes on account of the added complexity it brings, a.k.a. the ['diamond problem'](https://en.wikipedia.org/wiki/Multiple_inheritance#The_diamond_problem). In dynamically-typed languages, it's quite common to bolt on extra functionality to a class in a reusable way with [mixins](https://en.wikipedia.org/wiki/Mixin). As C# is a statically-typed language, it doesn't support mixins as a first-class language feature.

Uno can however, generate code. 

With Roslyn, Microsoft open-sourced the C# compiler, but they also exposed a powerful API for code analysis. Roslyn provides a easy to access all the syntactic and semantic information that the compiler possesses. Uno created a [source generator](https://github.com/nventive/Uno.SourceGeneration) that leverages this power for code generation and like the Uno platform, it's free and open-source.

Inside of the [Uno.SourceGeneration](https://github.com/nventive/Uno.SourceGeneration) you'll find a msbuild task that allows you to easily add generated code based on Roslyn's analysis of your solution. This might be partial class definitions which augment existing types, or it might be entirely new classes. 

In Uno, this used by the [DependencyObjectGenerator](https://github.com/nventive/Uno/blob/master/src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs) class. This generator looks for every class in the solution that implements the `DependencyObject` interface. For each such class, it automatically generates the methods and properties of `DependencyObject`. 

Since the generator has a full set of semantic information from Roslyn, it can do this in a smart way. For instance, if it detects that the class is a view type, it adds methods to update binding information when the view is loaded or unloaded. 

Here's a [small snippet](https://github.com/nventive/Uno/blob/74ba91756c446107e7394e0423527de273154f5d/src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs#L218-L250) of code from `DependencyObjectGenerator` which demonstrates this. In the snippet there is a [INamedTypeSymbol](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.inamedtypesymbol?view=roslyn-dotnet), an object from Roslyn that encapsulates information about a type and the following logic:

- A determination that `typeSymbol` implements `DependencyObject`.
- A check if it's an Android `View` and, if so override the loaded method. 
- A check to ensure the type doesn't _already_ override the same method so that the generator doesn't accidentally generate code that clashes with authored code and causes a compiler error. 

All this goes on under the hood without user intervention, whenever your app compiles. The end result is that `DependencyObject` can be used almost exactly the same way with Uno as with UWP, even though it's an interface and not a class.
