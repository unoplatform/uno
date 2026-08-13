using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.UnitTests")]
[assembly: InternalsVisibleTo("Uno.UI.Extras")]
[assembly: InternalsVisibleTo("Uno.UI.Composition")]

[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Win32")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Linux.FrameBuffer")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Headless")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.MacOS")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.X11")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Android")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.AppleUIKit")]

[assembly: InternalsVisibleTo("Uno.UI.MediaPlayer.Skia.X11")]
[assembly: InternalsVisibleTo("Uno.UI.MediaPlayer.Skia.Win32")]

[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.macOS")]

[assembly: InternalsVisibleTo("Uno.WinUI.Graphics3DGL")]

[assembly: InternalsVisibleTo("Uno")]

[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
