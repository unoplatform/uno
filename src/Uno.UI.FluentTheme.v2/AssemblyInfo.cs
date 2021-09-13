using global::System.Reflection;
using global::System.Runtime.CompilerServices;
using global::System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("Uno")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm")]
[assembly: InternalsVisibleTo("Uno.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.Tests")]

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
