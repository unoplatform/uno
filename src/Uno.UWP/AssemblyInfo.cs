using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Uno.UI")]
[assembly: InternalsVisibleTo("Uno.UI.Wasm")]
[assembly: InternalsVisibleTo("Uno.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("Uno.UI.Tests")]

#if __IOS__
[assembly: Foundation.LinkerSafe]
#elif __ANDROID__
[assembly: Android.LinkerSafe]
#endif
