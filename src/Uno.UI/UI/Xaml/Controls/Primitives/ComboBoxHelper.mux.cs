// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference controls/dev/ComboBox/ComboBoxHelper.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

#nullable enable

using Microsoft.UI.Dispatching;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ComboBoxHelper
{
	private const string c_popupBorderName = "PopupBorder";
	private const string c_editableTextName = "EditableText";
	private const string c_overlayCornerRadiusKey = "OverlayCornerRadius";

	internal ComboBoxHelper()
	{
	}

	// Normal ComboBox and editable ComboBox have different CornerRadius behaviors.
	// Xaml is not lifted yet when we implementing this feature so we don't have access to ComboBox code.
	// Creating this attached property to help us plug in some extra logic without touching the actual ComboBox code.
	private static void OnKeepInteriorCornersSquarePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		if (sender is ComboBox comboBox)
		{
			var shouldMonitorDropDownState = (bool)args.NewValue;
			if (shouldMonitorDropDownState)
			{
				var revokers = CreateDropDownEventRevokers(comboBox);
				SetDropDownEventRevokers(comboBox, revokers);
			}
			else
			{
				SetDropDownEventRevokers(comboBox, null);
			}
		}
	}

	private static void OnDropDownOpened(object? sender, object args)
	{
		var comboBox = (ComboBox)sender!;
		// We need to know whether the dropDown opens above or below the ComboBox in order to update corner radius correctly.
		// Sometimes TransformToPoint value is incorrect because popup is not fully opened when this function gets called.
		// Use dispatcher to make sure we get correct VerticalOffset.
		if (DispatcherQueue.GetForCurrentThread() is { } dispatcherQueue)
		{
			dispatcherQueue.TryEnqueue(() => UpdateCornerRadius(comboBox, isDropDownOpen: true));
		}
	}

	private static void OnDropDownClosed(object? sender, object args)
	{
		var comboBox = (ComboBox)sender!;
		UpdateCornerRadius(comboBox, isDropDownOpen: false);
	}

	private static void UpdateCornerRadius(ComboBox comboBox, bool isDropDownOpen)
	{
		if (comboBox.IsEditable)
		{
			var popupRadius = (CornerRadius)ResourceAccessor.ResourceLookup(comboBox, c_overlayCornerRadiusKey);
			var textBoxRadius = comboBox.CornerRadius;

			if (isDropDownOpen)
			{
				var isOpenDown = IsPopupOpenDown(comboBox);
				var popupRadiusFilter = isOpenDown ? CornerRadiusFilterKind.Bottom : CornerRadiusFilterKind.Top;
				popupRadius = CornerRadiusFilterConverter.Convert(popupRadius, popupRadiusFilter);

				var textBoxRadiusFilter = isOpenDown ? CornerRadiusFilterKind.Top : CornerRadiusFilterKind.Bottom;
				textBoxRadius = CornerRadiusFilterConverter.Convert(textBoxRadius, textBoxRadiusFilter);
			}

			if (comboBox.GetTemplateChild(c_popupBorderName) is Border popupBorder)
			{
				popupBorder.CornerRadius = popupRadius;
			}

			if (comboBox.GetTemplateChild(c_editableTextName) is TextBox textBox)
			{
				textBox.CornerRadius = textBoxRadius;
			}
		}
	}

	private static bool IsPopupOpenDown(ComboBox comboBox)
	{
		double verticalOffset = 0;
		if (comboBox.GetTemplateChild(c_popupBorderName) is Border popupBorder)
		{
			if (comboBox.GetTemplateChild(c_editableTextName) is TextBox textBox)
			{
				var transform = popupBorder.TransformToVisual(textBox);
				var popupTop = transform.TransformPoint(new Point(0, 0));
				verticalOffset = popupTop.Y;
			}
		}

		return verticalOffset > 0;
	}
}
