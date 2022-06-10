using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.Tests")]
[assembly: InternalsVisibleTo("Uno.UI.Toolkit")]
[assembly: InternalsVisibleTo("Uno.UI.Composition")]

[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Droid")]
[assembly: InternalsVisibleTo("SamplesApp.macOS")]
[assembly: InternalsVisibleTo("SamplesApp.Wasm")]
[assembly: InternalsVisibleTo("SamplesApp.Skia")]

[assembly: InternalsVisibleTo("Uno")]

#if NET6_0_OR_GREATER
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
#elif __IOS__
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: Foundation.LinkerSafe]
#pragma warning restore CS0618 // Type or member is obsolete
[assembly: AssemblyMetadata("IsTrimmable", "True")]
#elif __ANDROID__
[assembly: Android.LinkerSafe]
#endif
