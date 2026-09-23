#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Extensions;
using Windows.System;

namespace DirectUI;

// MUX Reference CoreImports.h, commit 2b8c7757e
internal enum VisualRelativeKind
{
	Child = 0,
	Parent = 1,
	Root = 2,
}

internal static class CoreImports
{
	// MUX Reference CoreImports.cpp, commit 2b8c7757e — DependencyObject_GetVisualRelative.
	// Child is not ported: it has no caller in Uno.
	internal static DependencyObject? DependencyObject_GetVisualRelative(UIElement element, VisualRelativeKind relativeLinkKind)
	{
		ArgumentNullException.ThrowIfNull(element);

		return relativeLinkKind switch
		{
			VisualRelativeKind.Parent => element.GetParentInternal(),
			VisualRelativeKind.Root => element.GetTreeRoot(publicParentOnly: true),
			_ => throw new ArgumentOutOfRangeException(nameof(relativeLinkKind)),
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static VirtualKeyModifiers Input_GetKeyboardModifiers() => PlatformHelpers.GetKeyboardModifiers();
}
