#if false // TODO Should no longer be needed
using System;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls;

[Flags]
public enum AnimationContext
{
	None = 0,
	CollectionChangeAdd = 1,
	CollectionChangeRemove = 2,
	CollectionChangeReset = 4,
	LayoutTransition = 8,
}
#endif