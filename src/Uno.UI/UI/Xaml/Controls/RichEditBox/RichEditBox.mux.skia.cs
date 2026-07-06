// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichEditBox_Partial.cpp, commit b8cfb8490

#nullable enable

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class RichEditBox
	{
		//---------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Updates the visibility of the Header property. If Header and Header
		//      Template are not set, it should collapse the property.
		//
		//---------------------------------------------------------------------------
		private void UpdateHeaderPresenterVisibility()
		{
			var headerTemplate = HeaderTemplate;
			var header = Header;

			// TODO Uno: The WinUI implementation calls ConditionallyGetTemplatePartAndUpdateVisibility,
			// which lazily realizes the header presenter. Uno resolves the presenter in OnApplyTemplate
			// and mirrors TextBox's simpler visibility toggle here.
			if (_headerPresenter is { } headerPresenter)
			{
				headerPresenter.Visibility = (header is not null || headerTemplate is not null)
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}

		//---------------------------------------------------------------------------
		//
		//  Synopsis:
		//      Updates PlaceholderText visibility whenever text is updated
		//
		//---------------------------------------------------------------------------
		private void UpdatePlaceholderTextPresenterVisibility(bool isEnabled)
		{
			if (_placeholderTextPresenter is { } placeholderTextPresenter)
			{
				placeholderTextPresenter.Visibility = isEnabled
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}
	}
}
