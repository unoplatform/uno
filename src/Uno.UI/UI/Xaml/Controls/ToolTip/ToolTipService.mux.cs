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

			// Uno specific: reuse the implicit wrapper rather than create a ToolTip per change (e.g. a recycled list row). WinUI recreates it.
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

	private static ToolTip ConvertToToolTip(object objectIn)
	{
		if (objectIn is ToolTip explicitToolTip)
		{
			// Uno specific: handed out as an explicit ToolTip (possibly to a second owner), so it must no longer be reused in place.
			explicitToolTip.IsImplicitToolTip = false;
		}

		if (objectIn is not ToolTip toolTip)
		{
			if (objectIn is FrameworkElement frameworkElement)
			{
				var objectInParent = objectIn.GetParent();
				if (objectInParent is ToolTip parentToolTip)
				{
					// Uno specific: shared by more than one owner, so it must no longer be reused in place.
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
