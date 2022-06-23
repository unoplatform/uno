using global::System.Reflection;
using global::System.Runtime.CompilerServices;
using global::System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("Uno")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm")]
[assembly: InternalsVisibleTo("Uno.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.Tests")]

[assembly: AssemblyMetadata("IsTrimmable", "True")]

#if __IOS__
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: Foundation.LinkerSafe]
#pragma warning restore CS0618 // Type or member is obsolete
#elif __ANDROID__
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: Android.LinkerSafe]
#pragma warning restore CS0618 // Type or member is obsolete
#endif
