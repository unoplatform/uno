using global::System.Reflection;
using global::System.Runtime.CompilerServices;
using global::System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("Uno.UI.Dispatching")]
[assembly: InternalsVisibleTo("Uno")]
[assembly: InternalsVisibleTo("Uno.Foundation")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm")]
[assembly: InternalsVisibleTo("Uno.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.Tests")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm.Tests")]

#if NET6_0_OR_GREATER
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
#elif __IOS__
[assembly: Foundation.LinkerSafe]
#elif __ANDROID__
[assembly: Android.LinkerSafe]
#endif
