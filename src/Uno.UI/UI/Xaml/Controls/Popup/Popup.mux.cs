// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Popup.h, Popup.cpp

namespace Windows.UI.Xaml.Controls
{
	public partial class Popup
	{
		internal bool OnClosing()
		{
			var cancel = false;

			// TODO Uno: When popups are properly supported, flyout can prevent
			// closing.
			//if (IsFlyout())
			//{
			//	cancel = FlyoutBase.OnClosing(m_associatedFlyoutWeakRef));
			//}

			return cancel;
		}
	}
}
