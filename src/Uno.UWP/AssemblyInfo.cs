using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.WebAssembly")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.Tests")]
[assembly: InternalsVisibleTo("Uno.UI.Unit.Tests")]
[assembly: InternalsVisibleTo("Uno.UI.Toolkit")]
[assembly: InternalsVisibleTo("Uno.UI.Composition")]
[assembly: InternalsVisibleTo("Uno.UI.Lottie")]
[assembly: InternalsVisibleTo("Uno.UI.Svg")]
[assembly: InternalsVisibleTo("Uno.UI.MediaPlayer.Skia.Gtk")]
[assembly: InternalsVisibleTo("Uno.UI.MediaPlayer.WebAssembly")]

[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Droid")]
[assembly: InternalsVisibleTo("SamplesApp.macOS")]
[assembly: InternalsVisibleTo("SamplesApp.Wasm")]
[assembly: InternalsVisibleTo("SamplesApp.Skia")]
[assembly: InternalsVisibleTo("SamplesApp.Skia.Linux.FrameBuffer")]
[assembly: InternalsVisibleTo("UnoIslandsSamplesApp.Skia")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
