using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests.Windows")]
[assembly: InternalsVisibleTo("Uno.UI.UnitTests")]
[assembly: InternalsVisibleTo("Uno.UI.Extras")]
[assembly: InternalsVisibleTo("Uno.UI.Composition")]

[assembly: InternalsVisibleTo("Uno.UI.Runtime")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Win32")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Linux.FrameBuffer")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Headless")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Android")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.AppleUIKit")]

[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Windows")]

[assembly: InternalsVisibleTo("Uno.WinUI.Graphics2DSK")]
[assembly: InternalsVisibleTo("Uno.UI.Lottie")]
[assembly: InternalsVisibleTo("Uno.WinUI.Graphics3DGL")]

[assembly: InternalsVisibleTo("UnoIslandsSamplesApp")]
[assembly: InternalsVisibleTo("UnoIslandsSamplesApp.Skia")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
