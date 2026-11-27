// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference MenuBarItemFlyout.cpp, MenuBarItemFlyout.h

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class MenuBarItemFlyout : MenuFlyout
	{
		internal Control m_presenter;

		public MenuBarItemFlyout() : base()
		{
		}

		protected override Control CreatePresenter()
		{
			m_presenter = base.CreatePresenter();
			return m_presenter;
		}
	}
}
