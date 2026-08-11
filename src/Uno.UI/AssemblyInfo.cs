using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Uno.Foundation.Diagnostics.CodeAnalysis;
using Uno.Foundation.Extensibility;
using Uno.UI.Graphics;

[assembly: InternalsVisibleTo("Uno.UI.Foldable")]
[assembly: InternalsVisibleTo("Uno.UI.UnitTests")]
[assembly: InternalsVisibleTo("Uno.UI.Extras")]
[assembly: InternalsVisibleTo("Uno.UI.RemoteControl")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests.Skia")]
[assembly: InternalsVisibleTo("Uno.UI.Lottie")]
[assembly: InternalsVisibleTo("Uno.UI.Svg")]
[assembly: InternalsVisibleTo("Uno.UI.XamlHost")]
[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Droid")]
[assembly: InternalsVisibleTo("SamplesApp.macOS")]
[assembly: InternalsVisibleTo("SamplesApp.Skia")]
[assembly: InternalsVisibleTo("UnoIslandsSamplesApp.Skia")]
[assembly: InternalsVisibleTo("Uno.UI.FluentTheme")]
[assembly: InternalsVisibleTo("Uno.UI.FluentTheme.v1")]
[assembly: InternalsVisibleTo("Uno.UI.FluentTheme.v2")]
[assembly: InternalsVisibleTo("Uno.UI.MediaPlayer.Skia.X11")]
[assembly: InternalsVisibleTo("Uno.UI.MediaPlayer.Skia.Win32")]
[assembly: InternalsVisibleTo("Uno.UI.WebView.Skia.X11")]

[assembly: InternalsVisibleTo("Uno.UI.HotDesign.Client")]

[assembly: InternalsVisibleTo("Uno.WinUI.Graphics3DGL")]
[assembly: InternalsVisibleTo("Uno.WinUI.Graphics2DSK")]

[assembly: AssemblyMetadata("IsTrimmable", "True")]

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Uno.UI.RuntimeTypeMetadataUpdateHandler))]

[assembly: AdditionalLinkerHint("System.Dynamic.ExpandoObject")]
[assembly: AdditionalLinkerHint("System.Dynamic.DynamicObject")]


[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.MacOS")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Win32")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Tizen")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Linux.FrameBuffer")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Headless")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.X11")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests.HRApp")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Android")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.AppleUIKit")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests.HRApp.Skia")]
[assembly: InternalsVisibleTo("Uno.WinUI.SpellChecking")]
