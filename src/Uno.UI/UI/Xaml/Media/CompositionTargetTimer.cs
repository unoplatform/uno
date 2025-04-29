#nullable enable

using System;
using System.Threading;
using Uno.UI;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// Use a generic frame timer instead of the native one, generally 
/// in the context of desktop targets.
/// </summary>
internal partial class CompositionTargetTimer
{
	internal static void Start()
	{
#if __SKIA__
		StartInternal();
#else
		throw new PlatformNotSupportedException();
#endif
	}
}
