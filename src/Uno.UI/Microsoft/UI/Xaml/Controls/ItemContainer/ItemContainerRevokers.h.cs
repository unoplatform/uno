// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemContainerRevokers.h, tag winui3/release/1.5.0

using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

internal partial class ItemContainerRevokers
{
	public void RevokeAll(ItemContainer itemContainer)
	{
		if (m_isSelectedPropertyChangedRevoker.Disposable is not null)
		{
			m_isSelectedPropertyChangedRevoker.Disposable = null;
		}

		if (m_gettingFocusRevoker.Disposable is not null)
		{
			m_gettingFocusRevoker.Disposable = null;
		}

		if (m_losingFocusRevoker.Disposable is not null)
		{
			m_losingFocusRevoker.Disposable = null;
		}

		if (m_keyDownRevoker.Disposable is not null)
		{
			m_keyDownRevoker.Disposable = null;
		}

		if (m_itemInvokedRevoker.Disposable is not null)
		{
			m_itemInvokedRevoker.Disposable = null;
		}

#if DEBUG
		if (m_sizeChangedRevokerDbg.Disposable is not null)
		{
			m_sizeChangedRevokerDbg.Disposable = null;
		}
#endif
	}

	internal SerialDisposable m_itemInvokedRevoker = new();
	internal SerialDisposable m_keyDownRevoker = new();
	internal SerialDisposable m_gettingFocusRevoker = new();
	internal SerialDisposable m_losingFocusRevoker = new();

	internal SerialDisposable m_isSelectedPropertyChangedRevoker = new();

#if DEBUG
	internal SerialDisposable m_sizeChangedRevokerDbg = new();
#endif
};
