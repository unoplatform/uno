// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MUXControlsTestApp.Samples
{
	public sealed partial class ElementsInItemsSourcePage : Page
	{
		public ElementsInItemsSourcePage()
		{
			this.InitializeComponent();
			goBackButton.Click += delegate { Frame.GoBack(); };
		}
	}

	public class UICollection : ObservableCollection<UIElement>
	{
		public UICollection()
		{

		}
	}
}
