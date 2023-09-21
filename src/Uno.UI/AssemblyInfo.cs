using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Uno.Foundation.Diagnostics.CodeAnalysis;

[assembly: InternalsVisibleTo("Uno.UI.Foldable")]
[assembly: InternalsVisibleTo("Uno.UI.Tests")]
[assembly: InternalsVisibleTo("Uno.UI.Unit.Tests")]
[assembly: InternalsVisibleTo("Uno.UI.Tests.Performance")]
[assembly: InternalsVisibleTo("Uno.UI.Toolkit")]
[assembly: InternalsVisibleTo("Uno.UI.RemoteControl")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.WebAssembly")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests.Skia")]
[assembly: InternalsVisibleTo("Uno.UI.Lottie")]
[assembly: InternalsVisibleTo("Uno.UI.Svg")]
[assembly: InternalsVisibleTo("Uno.UI.Svg.Skia")]
[assembly: InternalsVisibleTo("Uno.UI.XamlHost")]
[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Droid")]
[assembly: InternalsVisibleTo("SamplesApp.macOS")]
[assembly: InternalsVisibleTo("SamplesApp.Wasm")]
[assembly: InternalsVisibleTo("SamplesApp.Skia")]
[assembly: InternalsVisibleTo("SamplesApp.netcoremobile")]
[assembly: InternalsVisibleTo("UnoIslandsSamplesApp.Skia")]
[assembly: InternalsVisibleTo("Uno.UI.FluentTheme")]
[assembly: InternalsVisibleTo("Uno.UI.FluentTheme.v1")]
[assembly: InternalsVisibleTo("Uno.UI.FluentTheme.v2")]
[assembly: InternalsVisibleTo("Uno.UI.MediaPlayer.Skia.Gtk")]
[assembly: InternalsVisibleTo("Uno.UI.MediaPlayer.WebAssembly")]

[assembly: AssemblyMetadata("IsTrimmable", "True")]

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Uno.UI.RuntimeTypeMetadataUpdateHandler))]

[assembly: AdditionalLinkerHint("System.Dynamic.ExpandoObject")]
[assembly: AdditionalLinkerHint("System.Dynamic.DynamicObject")]
