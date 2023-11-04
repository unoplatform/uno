// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference MenuBar.properties.cpp, tag winui3/release/1.4.2

using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class MenuBar
	{
		public IList<MenuBarItem> Items
		{
			get => (IList<MenuBarItem>)this.GetValue(ItemsProperty);
			private set => this.SetValue(ItemsProperty, value);
		}

		public static DependencyProperty ItemsProperty { get; } =
			DependencyProperty.Register(
				"Items",
				typeof(IList<MenuBarItem>),
				typeof(MenuBar),
				new FrameworkPropertyMetadata(null)
			);
	}
}
