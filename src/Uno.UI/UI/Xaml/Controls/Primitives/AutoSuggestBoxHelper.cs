// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBoxHelper.cpp, tag winui3/release/1.7.1

using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Helper class for AutoSuggestBox that manages corner radius updates when the popup opens/closes.
/// </summary>
public partial class AutoSuggestBoxHelper
{
	private const string c_popupName = "SuggestionsPopup";
	private const string c_popupBorderName = "SuggestionsContainer";
	private const string c_textBoxName = "TextBox";
	private const string c_overlayCornerRadiusKey = "OverlayCornerRadius";

	#region KeepInteriorCornersSquare Attached Property

	/// <summary>
	/// Identifies the KeepInteriorCornersSquare attached property.
	/// </summary>
	public static new DependencyProperty KeepInteriorCornersSquareProperty { get; } =
		DependencyProperty.RegisterAttached(
			"KeepInteriorCornersSquare",
			typeof(bool),
			typeof(AutoSuggestBoxHelper),
			new FrameworkPropertyMetadata(false, OnKeepInteriorCornersSquarePropertyChanged));

	/// <summary>
	/// Gets the value of the KeepInteriorCornersSquare attached property for a specified AutoSuggestBox.
	/// </summary>
	/// <param name="autoSuggestBox">The AutoSuggestBox from which the property value is read.</param>
	/// <returns>The KeepInteriorCornersSquare property value for the AutoSuggestBox.</returns>
	public static new bool GetKeepInteriorCornersSquare(AutoSuggestBox autoSuggestBox)
		=> (bool)autoSuggestBox.GetValue(KeepInteriorCornersSquareProperty);

	/// <summary>
	/// Sets the value of the KeepInteriorCornersSquare attached property for a specified AutoSuggestBox.
	/// </summary>
	/// <param name="autoSuggestBox">The AutoSuggestBox to which the attached property is written.</param>
	/// <param name="value">The value to set.</param>
	public static new void SetKeepInteriorCornersSquare(AutoSuggestBox autoSuggestBox, bool value)
		=> autoSuggestBox.SetValue(KeepInteriorCornersSquareProperty, value);

	#endregion

	#region AutoSuggestEventRevokers Attached Property (Internal)

	private static readonly DependencyProperty AutoSuggestEventRevokersProperty =
		DependencyProperty.RegisterAttached(
			"AutoSuggestEventRevokers",
			typeof(AutoSuggestEventRevokers),
			typeof(AutoSuggestBoxHelper),
			new FrameworkPropertyMetadata(null));

	private static AutoSuggestEventRevokers GetAutoSuggestEventRevokers(AutoSuggestBox autoSuggestBox)
		=> (AutoSuggestEventRevokers)autoSuggestBox.GetValue(AutoSuggestEventRevokersProperty);

	private static void SetAutoSuggestEventRevokers(AutoSuggestBox autoSuggestBox, AutoSuggestEventRevokers value)
		=> autoSuggestBox.SetValue(AutoSuggestEventRevokersProperty, value);

	#endregion

	/// <summary>
	/// Called when the KeepInteriorCornersSquare property changes.
	/// </summary>
	private static void OnKeepInteriorCornersSquarePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		if (sender is AutoSuggestBox autoSuggestBox)
		{
			bool shouldMonitorAutoSuggestEvents = (bool)args.NewValue;

			if (shouldMonitorAutoSuggestEvents)
			{
				var revokers = new AutoSuggestEventRevokers();

				// Subscribe to Loaded event
				void OnLoaded(object s, RoutedEventArgs e) => OnAutoSuggestBoxLoaded(s, e);
				autoSuggestBox.Loaded += OnLoaded;
				revokers.LoadedRevoker = Disposable.Create(() => autoSuggestBox.Loaded -= OnLoaded);

				SetAutoSuggestEventRevokers(autoSuggestBox, revokers);
			}
			else
			{
				// Clear revokers to unsubscribe from events
				var revokers = GetAutoSuggestEventRevokers(autoSuggestBox);
				revokers?.Dispose();
				SetAutoSuggestEventRevokers(autoSuggestBox, null);
			}
		}
	}

	/// <summary>
	/// Called when the AutoSuggestBox is loaded.
	/// </summary>
	private static void OnAutoSuggestBoxLoaded(object sender, RoutedEventArgs args)
	{
		if (sender is not AutoSuggestBox autoSuggestBox)
		{
			return;
		}

		var revokers = GetAutoSuggestEventRevokers(autoSuggestBox);
		if (revokers is null)
		{
			return;
		}

		// Only set up popup events if not already set up
		if (revokers.PopupOpenedRevoker is null || revokers.PopupClosedRevoker is null)
		{
			var popup = autoSuggestBox.GetTemplateChild(c_popupName) as Popup;
			if (popup is not null)
			{
				// Use weak reference to avoid preventing garbage collection
				var autoSuggestBoxWeakRef = new WeakReference<AutoSuggestBox>(autoSuggestBox);

				void OnPopupOpened(object s, object e)
				{
					if (autoSuggestBoxWeakRef.TryGetTarget(out var asb))
					{
						UpdateCornerRadius(asb, isPopupOpen: true);
					}
				}

				void OnPopupClosed(object s, object e)
				{
					if (autoSuggestBoxWeakRef.TryGetTarget(out var asb))
					{
						UpdateCornerRadius(asb, isPopupOpen: false);
					}
				}

				popup.Opened += OnPopupOpened;
				popup.Closed += OnPopupClosed;

				revokers.PopupOpenedRevoker = Disposable.Create(() => popup.Opened -= OnPopupOpened);
				revokers.PopupClosedRevoker = Disposable.Create(() => popup.Closed -= OnPopupClosed);
			}
		}
	}

	/// <summary>
	/// Updates the corner radius of the TextBox and popup border based on popup state.
	/// </summary>
	private static void UpdateCornerRadius(AutoSuggestBox autoSuggestBox, bool isPopupOpen)
	{
		// Try to get the OverlayCornerRadius resource
		CornerRadius popupRadius = default;
		if (autoSuggestBox.Resources.TryGetValue(c_overlayCornerRadiusKey, out var resource) && resource is CornerRadius cr)
		{
			popupRadius = cr;
		}
		else if (Application.Current?.Resources.TryGetValue(c_overlayCornerRadiusKey, out resource) == true && resource is CornerRadius cr2)
		{
			popupRadius = cr2;
		}

		var textBoxRadius = autoSuggestBox.CornerRadius;

		if (isPopupOpen)
		{
			var isOpenDown = IsPopupOpenDown(autoSuggestBox);

			var popupRadiusFilter = isOpenDown ? CornerRadiusFilterKind.Bottom : CornerRadiusFilterKind.Top;
			popupRadius = CornerRadiusFilterConverter.Convert(popupRadius, popupRadiusFilter);

			var textBoxRadiusFilter = isOpenDown ? CornerRadiusFilterKind.Top : CornerRadiusFilterKind.Bottom;
			textBoxRadius = CornerRadiusFilterConverter.Convert(textBoxRadius, textBoxRadiusFilter);
		}

		// Update popup border corner radius
		if (autoSuggestBox.GetTemplateChild(c_popupBorderName) is Border popupBorder)
		{
			popupBorder.CornerRadius = popupRadius;
		}

		// Update textbox corner radius
		if (autoSuggestBox.GetTemplateChild(c_textBoxName) is TextBox textBox)
		{
			textBox.CornerRadius = textBoxRadius;
		}
	}

	/// <summary>
	/// Determines if the popup is opening downward (below the TextBox).
	/// </summary>
	private static bool IsPopupOpenDown(AutoSuggestBox autoSuggestBox)
	{
		double verticalOffset = 0;

		var popupBorder = autoSuggestBox.GetTemplateChild(c_popupBorderName) as FrameworkElement;
		var textBox = autoSuggestBox.GetTemplateChild(c_textBoxName) as FrameworkElement;

		if (popupBorder is not null && textBox is not null)
		{
			try
			{
				var transform = popupBorder.TransformToVisual(textBox);
				var popupTop = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
				verticalOffset = popupTop.Y;
			}
			catch
			{
				// Transform may fail if elements are not in the same visual tree
			}
		}

		return verticalOffset >= 0;
	}

	/// <summary>
	/// Internal class to hold event revokers for AutoSuggestBox events.
	/// </summary>
	private sealed class AutoSuggestEventRevokers : IDisposable
	{
		public IDisposable LoadedRevoker { get; set; }
		public IDisposable PopupOpenedRevoker { get; set; }
		public IDisposable PopupClosedRevoker { get; set; }

		public void Dispose()
		{
			LoadedRevoker?.Dispose();
			PopupOpenedRevoker?.Dispose();
			PopupClosedRevoker?.Dispose();
		}
	}
}
