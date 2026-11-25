// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\ToolTipService_Partial.cpp, tag winui3/release/1.5.4, commit 98a60c8f30c84f297a175dd2884d54ecd1c8a4a9
// Contains ported portions of dxaml\xcp\dxaml\lib\ToolTipService_Partial.cpp

#nullable enable

using static Uno.UI.FeatureConfiguration;

namespace Microsoft.UI.Xaml.Controls;

partial class ToolTipService
{
	private static void OnToolTipChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		if (sender is FrameworkElement senderAsFe)
		{
			bool isKeyboardAcceleratorToolTip = args.Property == ToolTipService.KeyboardAcceleratorToolTipProperty;

			// Uno specific: reuse the implicit wrapper rather than create a ToolTip per change (e.g. a recycled list row).
			if (args.OldValue is not UnsetValue && args.OldValue is not null
				&& args.NewValue is { } newValue
				&& TryUpdateImplicitToolTip(sender, senderAsFe, args.Property, newValue, isKeyboardAcceleratorToolTip))
			{
				return;
			}

			if (args.OldValue is not UnsetValue && args.OldValue is not null)
			{
				ToolTipService.UnregisterToolTip(sender, senderAsFe, isKeyboardAcceleratorToolTip);
			}

			if (args.NewValue is { } toolTip)
			{
				ToolTipService.RegisterToolTip(sender, senderAsFe, toolTip, isKeyboardAcceleratorToolTip);
			}
		}
	}

	private static bool TryUpdateImplicitToolTip(
		DependencyObject owner,
		FrameworkElement container,
		DependencyProperty property,
		object newValue,
		bool isKeyboardAcceleratorToolTip)
	{
		if (!global::Uno.UI.FeatureConfiguration.ToolTip.UseToolTips)
		{
			// As on the regular path: the old ToolTip is unregistered and no new one is registered.
			return false;
		}

		var current = isKeyboardAcceleratorToolTip
			? ToolTipService.GetKeyboardAcceleratorToolTipObject(owner)
			: ToolTipService.GetToolTipReference(owner);

		if (current is not { IsImplicitToolTip: true }
			|| newValue is ToolTip
			|| (newValue is FrameworkElement newElement && newElement.GetParent() is ToolTip))
		{
			// Either there's no wrapper to reuse, or ConvertToToolTip would not create a new wrapper for this value.
			return false;
		}

		// Same as the unregister path: a change of value closes the tooltip (and stops its timers).
		CloseToolTipImpl(current);

		if (!Equals(owner.GetValue(property), newValue))
		{
			// A Closed handler changed the value again; that nested change has already been applied.
			return true;
		}

		// As RegisterToolTip does on every change: PlacementTarget has no change callback, so this is where it's picked up.
		current.SetAnchor(GetPlacementTarget(container) ?? container);
		current.Content = newValue;
		return true;
	}

	private static ToolTip ConvertToToolTip(object objectIn)
	{
		if (objectIn is not ToolTip toolTip)
		{
			if (objectIn is FrameworkElement frameworkElement)
			{
				var objectInParent = objectIn.GetParent();
				if (objectInParent is ToolTip parentToolTip)
				{
					// Uno specific: the ToolTip is now shared by more than one owner, so a value change on one owner must not
					// change it in place for the others.
					parentToolTip.IsImplicitToolTip = false;
					return parentToolTip;
				}
			}

			var newToolTip = new ToolTip
			{
				Content = objectIn,
				IsImplicitToolTip = true,
			};

			toolTip = newToolTip;
		}

		return toolTip;
	}

	internal static ToolTip? GetActualToolTipObject(DependencyObject element)
	{
		// Try to get the actual public tooltip object
		var toolTip = ToolTipService.GetToolTipReference(element);

		// If public tooltip doesn't exist, then look for keyboard accelerator tooltip.
		if (toolTip is null)
		{
			toolTip = ToolTipService.GetKeyboardAcceleratorToolTipObject(element);
		}

		return toolTip;
	}
}
