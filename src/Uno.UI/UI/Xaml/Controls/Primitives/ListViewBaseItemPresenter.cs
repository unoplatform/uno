// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference XamlOM/Model/Microsoft.UI.Xaml.Controls.Primitives.cs, tag winui3/release/2.5.1
// MUX Reference DependencyProperty.cpp, tag winui3/release/2.5.1

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

// UNO ONLY: public because subclasses are public; WinUI hides it from IDL.
// All members of this class MUST be internal (or private protected).
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

	private protected override void OnCornerRadiusChanged(CornerRadius oldValue, CornerRadius newValue)
	{
		base.OnCornerRadiusChanged(oldValue, newValue);
		OnPropertyChangedNewStyle(new DependencyPropertyChangedEventArgs
		{
			PropertyInternal = CornerRadiusProperty,
			OldValueInternal = oldValue,
			NewValueInternal = newValue,
		});
	}

	// TODO Uno: CListViewBaseItemChrome::OnPropertyChangedNewStyle (C:5132-5157) arrives with the chrome port.
	private protected virtual void OnPropertyChangedNewStyle(DependencyPropertyChangedEventArgs args)
	{
	}
}
