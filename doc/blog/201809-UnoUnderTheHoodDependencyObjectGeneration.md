# Talkin' 'bout my generation: How the Uno Platform generates code, part 2

[Previously](https://medium.com/@unoplatform/talkin-bout-my-generation-how-the-uno-platform-generates-code-part-1-under-the-hood-7664d83c4f90) we looked at how the [Uno Platform](https://platform.uno/) turns XAML mark-up files into C# code.  In this article, I'll talk about another way Uno uses code generation, allowing us to make native Android and iOS views conform to UWP's API, and tackle the thorny problem of [multiple inheritance](https://en.wikipedia.org/wiki/Multiple_inheritance). 
 
## Wanting it all
Part of the power of Uno on Android and iOS comes from the ability to mix view types from the UWP framework with purely native views. But this poses a challenge for reproducing UWP's inheritance hierarchy: as I mentioned in an [earlier article](https://hackernoon.com/pushing-the-right-buttons-how-uno-implements-views-under-the-hood-a5e93ea86688), `UIElement` is the primitive view type in UWP, but it in turn derives from the `DependencyObject` class. `DependencyObject` exposes methods related to the dependency property/databinding system. 
 
Multiple inheritance isn't permitted in C#, so in order for UIElement to be a `ViewGroup` on Android and a `UIView` on iOS, and still 'be' a `DependencyObject` as well, we opted to make `DependencyObject` an interface. 
 
But that alone doesn't solve our problems. What if consumer code wants to inherit directly from `DependencyObject`? The following is completely valid in an Uno/UWP app:

```` csharp
public MyBindableObject : DependencyObject { 
    // Custom dependency properties... 
}
````
 
There are non-view framework types, too, which inherit from `DependencyObject`, like `Transforms` and `Brushes`. 
 
We face a weaker form of this problem - wanting to have two base types - in other cases is well. In a few places in the framework we inherit from a more derived native view type. For example, `ScrollContentPresenter` inherits from the native scroll view on Android and iOS. But we also want `ScrollContentPresenter` to expose the methods and properties of `FrameworkElement`. 
 
## Mixing it up 
Most statically-typed languages don't permit multiple base classes on account of the added complexity it brings (the 'diamond problem'). (C++ is a notable exception.) In dynamically-typed languages though it's quite common to bolt on extra functionality to a class in a reusable way with [mixins](https://en.wikipedia.org/wiki/Mixin).  
 
C#, as a statically-typed language, doesn't support mixins as a first-class language feature. Code generation allows us to simulate it, though. Uno  uses code generation to add mixins in (at least) two different ways. 
 
I'll start with the simpler approach: using 'T4' templates. To quote Microsoft's documentation: 
> In Visual Studio, a T4 text template is a mixture of text blocks and control logic that can generate a text file. The control logic is written as fragments of program code in Visual C# or Visual Basic. In Visual Studio 2015 Update 2 and later, you can use C# version 6.0 features in T4 templates directives. The generated file can be text of any kind, such as a web page, or a resource file, or program source code in any language. 
 
*Source:* https://docs.microsoft.com/en-us/visualstudio/modeling/code-generation-and-t4-text-templates?view=vs-2017
 
T4 templates ('.tt files') have been around for quite a while. They're essentially a mix of static text (which is C# code, in our case) and conditional logic. Here's a snippet:

```` csharp
namespace <#= mixin.NamespaceName #> 
{ 
    public partial class <#= mixin.ClassName #> : IFrameworkElement 
    {  
    #if !<#= mixin.IsFrameworkElement #> 
        /// <summary> 
        /// Gets the parent of this FrameworkElement in the object tree. 
        /// </summary> 
        public DependencyObject Parent => ((IDependencyObjectStoreProvider)this).Store.Parent as DependencyObject; 
#endif 
 
#if <#= mixin.HasAttachedToWindow #> 
    partial void OnAttachedToWindowPartial() 
    { 
        OnLoading(); 
        OnLoaded(); 
    } 
…
````
 
That's from the [template](https://github.com/nventive/Uno/blob/be4f4e938a861d5802c228efc314c1f3ea314027/src/Uno.UI/UI/Xaml/IFrameworkElementImplementation.iOS.tt#L30-L46) which adds `IFrameworkElement` functionality in Uno. It implements properties like `Width`/`Height`, `Opacity`, `Style`, etc. At compile time, the template runs and creates a [partial class](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods) with those members for `ScrollContentPresenter` and several other classes (including `FrameworkElement` itself).  
 
The T4 approach is well-tested and works well in this scenario. It has a couple of limitations though: 
 
1. It requires manual set-up: each class that wants to use the mixin has to be explicitly registered. 
2. It requires manual flags to make sure that the generated code doesn't 'step on' the authored code, eg by generating a `Foo()` method when the authored code already defines `Foo()`.  
3. It doesn't support external code. You can't use the mixin above in your app (short of copy-pasting the templates into the app). 
 
For that reason, in order to have a mixin to implement `DependencyObject`'s features, we went with something a little more complex and a little more magical. 
 
## Making the magic happen 
The release of [Roslyn](https://github.com/dotnet/roslyn), aka the '.NET Compiler Platform', was a boon to code generation. With Roslyn, Microsoft open-sourced the C# compiler, but they also exposed a powerful API for code analysis. With Roslyn it's easy to access all the syntactic and semantic information that the compiler possesses.  
 
To leverage this power for code generation, we created the [Uno.SourceGeneration](https://github.com/nventive/Uno.SourceGeneration) package. Like the Uno Platform, it's free and open source. It creates a build task and allows you to easily add generated code based on Roslyn's analysis of your solution. This might be partial class definitions which augment existing types, or it might be entirely new classes. 
 
In Uno, this used by the [DependencyObjectGenerator](https://github.com/nventive/Uno/blob/master/src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs) class. This generator looks for every class in the solution that implements the `DependencyObject` interface, like our `MyBindableObject` example above. For each such class, it automatically generates the methods and properties of `DependencyObject`. Since it has all the information from Roslyn, it can do this in a smart way: for instance, if it detects that the class is a view type, it adds methods to update binding information when the view is loaded or unloaded. 
 
The end result is that `DependencyObject` can be used almost exactly the same way with Uno as with UWP, even though it's an interface and not a class! There are edge cases: some generic constraints won't work the same way, for example. But in general it works remarkably well. 
 
 ---

That's all for now. Let us know what other 'under the hood' aspects of Uno you'd like to hear more about!
