// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference XamlOM/Model/Microsoft.UI.Xaml.Controls.Primitives.cs, tag winui3/release/2.5.1
// MUX Reference DependencyProperty.cpp, tag winui3/release/2.5.1

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

// UNO ONLY: public because subclasses are public; WinUI hides it from IDL.
// All new members of this class MUST be internal (or private protected); overrides of existing public API are fine.
public abstract partial class ListViewBaseItemPresenter : ContentPresenter
{
	// Ports CDependencyProperty::GetDefaultValue for the presenter DPs (DependencyProperty.cpp:159-193).
	internal override bool GetDefaultValue2(DependencyProperty property, out object defaultValue)
	{
		if (property == ListViewItemPresenter.DisabledOpacityProperty
			|| property == ListViewItemPresenter.SelectedBorderThicknessProperty
			|| property == ListViewItemPresenter.SelectionIndicatorVisualEnabledProperty
			|| property == GridViewItemPresenter.DisabledOpacityProperty
			|| property == GridViewItemPresenter.SelectedBorderThicknessProperty)
		{
			// WinUI quirk: unlike the chrome readers, Deny here skips the resource lookup, so Deny + resource True gives non-rounded defaults on a rounded chrome.
			var denyRoundedListViewBaseItemChrome = ListViewBaseItemChromeRuntimeFeatures.DenyRoundedListViewBaseItemChrome;
			var forceRoundedListViewBaseItemChrome = ListViewBaseItemChromeRuntimeFeatures.ForceRoundedListViewBaseItemChrome;

			var forRoundedListViewBaseItemChrome = false;

			if (!denyRoundedListViewBaseItemChrome && !forceRoundedListViewBaseItemChrome)
			{
				forRoundedListViewBaseItemChrome = DependencyProperty.GetBooleanThemeResourceValue("ListViewBaseItemRoundedChromeEnabled");
			}
			else if (!denyRoundedListViewBaseItemChrome)
			{
				forRoundedListViewBaseItemChrome = true;
			}

			if (property == ListViewItemPresenter.SelectionIndicatorVisualEnabledProperty)
			{
				defaultValue = forRoundedListViewBaseItemChrome;
			}
			else if (property == ListViewItemPresenter.DisabledOpacityProperty || property == GridViewItemPresenter.DisabledOpacityProperty)
			{
				defaultValue = (double)ListViewBaseItemPresenter.GetDefaultDisabledOpacity(forRoundedListViewBaseItemChrome);
			}
			else
			{
				defaultValue = ListViewBaseItemPresenter.GetSelectedBorderXThickness(forRoundedListViewBaseItemChrome);
			}

			return true;
		}

		return base.GetDefaultValue2(property, out defaultValue);
	}

	// Backs the deprecated alias DPs, which have no storage: they read and write the ContentPresenter property.
	private protected static object? ForwardAlias(DependencyObject instance, bool isGet, object? valueToSet, DependencyProperty alias, DependencyProperty target)
	{
		if (isGet)
		{
			return instance.GetValue(target);
		}

		// The store replays the alias default when the alias value is cleared; forwarding it would clobber a TemplateBinding on the target.
		var precedence = instance.GetCurrentHighestValuePrecedence(alias);
		if (precedence != DependencyPropertyValuePrecedences.DefaultValue)
		{
			// TODO Uno: the store passes no incoming precedence (a value below the alias' current one lands higher), and keeps no Style value under a Local one on the target.
			instance.SetValue(target, valueToSet, precedence);
		}

		return null;
	}

	// The chrome renders the background layers; ContentPresenter.Background is never drawn, so no BrushTransition is set up for it.
	private protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
	{
	}

	// MUX Reference ListViewBaseItemChrome.cpp, lines 2288-2338 (HitTestLocalInternal): all hits within the bounds count.
	internal override bool IsViewHit() => true;

	internal override bool HitTest(Point point)
	{
		var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
		if (!bounds.Contains(point))
		{
			return false;
		}

		// MUX Reference uielement.cpp, lines 13345-13350: rounded corners clip the content and the children.
		if (RequiresCompNodeForRoundedCorners())
		{
			return IsInsideRoundedCorners(point, bounds.Size);
		}

		return true;
	}

	// TODO Uno: HitTestLocalInternalPostChildren (ListViewBaseItemChrome.cpp, lines 2340-2360) has no post-children hit test hook;
	// the presenter's own HitTest already covers its bounds.

	private bool IsInsideRoundedCorners(Point point, Size size)
	{
		var radii = CornerRadius.GetRadii(size, default).Outer;

		return IsInsideCorner(radii.TopLeft, radii.TopLeft.X - point.X, radii.TopLeft.Y - point.Y)
			&& IsInsideCorner(radii.TopRight, point.X - (size.Width - radii.TopRight.X), radii.TopRight.Y - point.Y)
			&& IsInsideCorner(radii.BottomRight, point.X - (size.Width - radii.BottomRight.X), point.Y - (size.Height - radii.BottomRight.Y))
			&& IsInsideCorner(radii.BottomLeft, radii.BottomLeft.X - point.X, point.Y - (size.Height - radii.BottomLeft.Y));

		// dx/dy: distance from the corner's ellipse center towards the corner, positive inside the corner square.
		static bool IsInsideCorner(global::System.Numerics.Vector2 radius, double dx, double dy)
		{
			if (radius.X <= 0 || radius.Y <= 0 || dx <= 0 || dy <= 0)
			{
				return true;
			}

			var nx = dx / radius.X;
			var ny = dy / radius.Y;
			return nx * nx + ny * ny <= 1;
		}
	}

	private protected override void OnCornerRadiusChanged(CornerRadius oldValue, CornerRadius newValue)
	{
		base.OnCornerRadiusChanged(oldValue, newValue);
		OnPropertyChangedNewStyle(new DependencyPropertyChangedEventArgs
		{
			PropertyInternal = CornerRadiusProperty,
			OldValueInternal = oldValue,
			NewValueInternal = newValue,
		});

		// Uno-specific: WinUI re-renders after UpdateRequiresCompNodeForRoundedCorners; the post layer depends on it.
		RenderLayers();
	}
}
