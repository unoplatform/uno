using global::System.Reflection;
using global::System.Runtime.CompilerServices;
using global::System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("Uno")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm")]
[assembly: InternalsVisibleTo("Uno.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.Tests")]

#if __IOS__
[assembly: Foundation.LinkerSafe]
#elif __ANDROID__
[assembly: Android.LinkerSafe]
#endif
