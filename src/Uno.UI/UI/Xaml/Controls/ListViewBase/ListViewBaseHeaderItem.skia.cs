// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBase_Partial_Interaction.cpp, commit dc46907e92

#nullable enable

using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

public partial class ListViewBaseHeaderItem
{
	protected override void OnTapped(TappedRoutedEventArgs args)
	{
		base.OnTapped(args);

		if (!args.Handled &&
			ItemsControl.ItemsControlFromItemContainer(this) is ListViewBase listView)
		{
			args.Handled = listView.OnHeaderItemTap(this);
		}
	}

	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		base.OnKeyDown(args);

		if (ItemsControl.ItemsControlFromItemContainer(this) is ListViewBase listView)
		{
			var isHandled = false;

			// for gamepad, this action happens on key up. Toggling sezo on KeyDown will cause a key up
			// in the other view and toggle it back.
			if (args.OriginalKey != VirtualKey.GamepadA)
			{
				isHandled = listView.OnGroupHeaderKeyDown(this, args.OriginalKey, args.Key);
			}

			if (isHandled)
			{
				args.Handled = true;
			}
		}
	}

	// Handle Key Up for gamepad and toggle
	protected override void OnKeyUp(KeyRoutedEventArgs args)
	{
		base.OnKeyUp(args);

		if (args.OriginalKey == VirtualKey.GamepadA &&
			ItemsControl.ItemsControlFromItemContainer(this) is ListViewBase listView &&
				listView.OnGroupHeaderKeyUp(this, args.OriginalKey, args.Key))
		{
			args.Handled = true;
		}
	}

	protected override void OnPointerReleased(PointerRoutedEventArgs args)
	{
		base.OnPointerReleased(args);

		// if we are in a semantic zoom, handle the pointer released key
		// so that the ScrollViewer above does not handle it and move focus
		// to itself.
		if (ItemsControl.ItemsControlFromItemContainer(this) is ListViewBase { SemanticZoomOwner: not null })
		{
			args.Handled = true;
		}
	}
}
