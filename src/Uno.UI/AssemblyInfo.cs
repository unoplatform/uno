using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Uno.Foundation.Diagnostics.CodeAnalysis;
using Uno.Foundation.Extensibility;

[assembly: InternalsVisibleTo("Uno.UI.Foldable")]
[assembly: InternalsVisibleTo("Uno.UI.UnitTests")]
[assembly: InternalsVisibleTo("Uno.UI.Extras")]
[assembly: InternalsVisibleTo("Uno.UI.RemoteControl")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests.Skia")]
[assembly: InternalsVisibleTo("Uno.UI.Lottie")]
[assembly: InternalsVisibleTo("Uno.UI.XamlHost")]
[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("UnoIslandsSamplesApp.Skia")]
[assembly: InternalsVisibleTo("Uno.UI.FluentTheme")]
[assembly: InternalsVisibleTo("Uno.UI.MediaPlayer.X11")]
[assembly: InternalsVisibleTo("Uno.UI.MediaPlayer.Win32")]
[assembly: InternalsVisibleTo("Uno.UI.WebView.X11")]

[assembly: InternalsVisibleTo("Uno.UI.HotDesign.Client")]

[assembly: InternalsVisibleTo("Uno.WinUI.Graphics3DGL")]
[assembly: InternalsVisibleTo("Uno.WinUI.Graphics2DSK")]

[assembly: AssemblyMetadata("IsTrimmable", "True")]

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Uno.UI.RuntimeTypeMetadataUpdateHandler))]

[assembly: AdditionalLinkerHint("System.Dynamic.ExpandoObject")]
[assembly: AdditionalLinkerHint("System.Dynamic.DynamicObject")]


[assembly: InternalsVisibleTo("Uno.UI.Runtime.MacOS")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Win32")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Linux.FrameBuffer")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Headless")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.X11")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests.HRApp")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Android")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.AppleUIKit")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests.HRApp.Skia")]
[assembly: InternalsVisibleTo("Uno.WinUI.SpellChecking")]
