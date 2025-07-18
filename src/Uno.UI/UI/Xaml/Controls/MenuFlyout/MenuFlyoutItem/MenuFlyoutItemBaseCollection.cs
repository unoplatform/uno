// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyoutItemBaseCollection_Partial.cpp, tag winui3/release/1.7.1, commit 5f27a786ac96c

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls;

internal class MenuFlyoutItemBaseCollection : DependencyObjectCollection<MenuFlyoutItemBase>
{
	private readonly DependencyObject _owner;

	public MenuFlyoutItemBaseCollection(DependencyObject owner) : base(owner)
	{
		_owner = owner;
	}

#if HAS_UNO // Our API is a bit different from WinUI
	private protected override void OnCollectionChanged() => NotifyMenuFlyoutOfCollectionChange();
#endif

	private void NotifyMenuFlyoutOfCollectionChange()
	{
		if (_owner is MenuFlyout menuFlyout)
		{
			menuFlyout.QueueRefreshItemsSource();
		}
		else if (_owner is MenuFlyoutSubItem menuFlyoutSubItem)
		{
			menuFlyoutSubItem.QueueRefreshItemsSource();
		}
		else
		{
			throw new InvalidOperationException("Unknown owner of MenuFlyoutItemBaseCollection.");
		}
	}
}
