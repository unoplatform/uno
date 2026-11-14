// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference CoreImports.h, CoreImports.cpp, commit 2b8c7757e

#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Extensions;
using Windows.System;

namespace DirectUI;

internal enum VisualRelativeKind
{
	Child = 0,
	Parent = 1,
	Root = 2,
}

internal static class CoreImports
{
	internal static DependencyObject? DependencyObject_GetVisualRelative(UIElement element, VisualRelativeKind relativeLinkKind)
	{
		ArgumentNullException.ThrowIfNull(element);

		return relativeLinkKind switch
		{
			VisualRelativeKind.Parent => element.GetParentInternal(),
			VisualRelativeKind.Root => element.GetTreeRoot(publicParentOnly: true),
			VisualRelativeKind.Child => throw new NotSupportedException("VisualRelativeKind.Child is not ported: it has no caller in Uno."),
			_ => throw new ArgumentOutOfRangeException(nameof(relativeLinkKind)),
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static VirtualKeyModifiers Input_GetKeyboardModifiers() => PlatformHelpers.GetKeyboardModifiers();
}
