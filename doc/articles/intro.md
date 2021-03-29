# Uno Platform documentation

<div class="row">

<div class="col-md-6 col-xs-12 ">
<a href="get-started.md">
<div class="alert alert-info">

#### Get started

Set up with your OS and IDE of choice.

</div>
</a>
</div>

<div class="col-md-6 col-xs-12 ">
<a href="tutorials-intro.md">
<div class="alert alert-info">

#### How-tos and tutorials

See real-world examples with working code.

</div>
</a>
</div>

<div class="col-md-6 col-xs-12 ">
<a href="using-uno-ui.md">
<div class="alert alert-info">

#### Developing with Uno Platform

Learn the principles of cross-platform development with Uno.

</div>
</a>
</div>

<div class="col-md-6 col-xs-12 ">
<a href="implemented-views.md">
<div class="alert alert-info">

#### API reference

Browse the set of available controls and their properties.

</div>
</a>
</div>

</div>

<br/>

Uno Platform lets you write an application once in XAML and C#, and deploy it to any target platform. 

<br/>
<br/>

***


## Top questions about Uno Platform

#### What platforms can I target with Uno Platform?

Uno Platform applications run on Web (via WebAssembly), Windows, Linux, macOS, iOS, Android and Tizen. [Check supported platform versions.](getting-started/requirements.md)

#### Are Uno Platform applications native?

Yes - Uno Platform taps into the native UI frameworks on most supported platforms, so your final product is a native app. [Read more about how Uno works.](what-is-uno.md)

#### Can applications look the same on all platforms?

Yes. Unless you specify otherwise, your application's UI renders exactly the same on all targeted platforms, to the pixel. Uno achieves this by taking low-level control of the native visual primitives on the targeted platform. [Read more about how Uno works.](what-is-uno.md)

#### How is Uno Platform different from Xamarin.Forms or .NET MAUI?

At a very high level, Uno Platform and Xamarin.Forms/.NET MAUI have different starting points and tap different ecosystems.  We believe WinUI as a starting point has the richest API set and styling engine which can be used to create pixel-perfect applications on all Web, Desktop and Mobile platforms. Xamarin.Forms and its successor .NET MAUI stem from ‘mobile origins’ and aim to bring a shared skills and tooling ecosystem to all platforms Xamarin.Forms currently supports, and more platforms in the future.

At a slightly lower technical level, Uno Platform and Xamarin.Forms/.NET MAUI are XAML-based UI frameworks, but the XAML 'dialect' used by Uno Platform is shared with Microsoft's Desktop (WinUI/UWP) framework, whereas Xamarin.Forms and MAUI have their own 'dialect' with different control names, syntaxes, etc. Uno Platform harnesses control templating ('lookless' controls) for pixel-perfect rendering, whereas Xamarin.Forms/MAUI opts for per-platform renderers to render controls on each supported platform.

Uno Platform runs on the Web and Linux today, in addition to the other supported platforms listed above. Xamarin.Forms/.NET MAUI currently officially support deployment to Android, iOS, and Windows, with additional community support for macOS and Linux.

Ultimately, at the practical level, we suggest you try both frameworks and see which works the best for your skillset and scenario. 

#### How is Uno Platform different from Blazor?

Uno Platform applications are written in C# and XAML markup, whereas Blazor applications are written in 'Razor' syntax, a hybrid of HTML/CSS and C#.

Uno Platform applications are cross-platform, running on the web and many other target platforms from a single codebase. Blazor is a feature of ASP.NET for building web applications.

Uno Platform and Blazor both make use of .NET's WebAssembly support to run natively in the browser.

#### How is Uno Platform different from Flutter?

Uno Platform supports C# and XAML markup for authoring applications. Uno Platform applications can use other .NET Standard packages within the broader .NET ecosystem. Flutter uses the Dart language, and doesn't have markup.

#### Do I need to have an existing UWP/WinUI app to use Uno?

No, there's no need to have an existing UWP or WinUI application, or have that specific skillset. The [Uno Platform templates](get-started.md) make it easy to create a new project in Visual Studio or from the command line for anyone familiar with C# and XAML. 

#### What 3rd parties support Uno Platform?

Uno Platform is supported by a number of 3rd-party packages and libraries, including advanced controls from Microsoft Windows Community Toolkit, SyncFusion and Infragistics; graphics processing with SkiaSharp; presentation and navigation with Prism, ReactiveUI and MVVMCross; local database management with SQLite; and more. [See the full list of supported 3rd-party libraries.](supported-libraries.md)

#### Where do I get help if I have any questions?

Community support is available through [Stack Overflow](https://stackoverflow.com/questions/tagged/uno-platform) and Discord www.platform.uno/discord - #uno-platform channel. Commercial paid support is available as well - email [info@platform.uno](mailto:info@platform.uno) for more information.

#### How do you sustain Uno Platform?

The Uno Platform is free and open source under the Apache license. Alongside valued contributions from the Uno community, development by the core team is sustained by paid professional support contracts offered to enterprises who use Uno Platform. [Learn more about our paid professional support.](https://platform.uno/contact/) 

More details about sustainability are covered here: https://platform.uno/blog/sustaining-the-open-source-uno-platform/ 


[_See more frequently asked questions about the Uno Platform._](faq.md)
