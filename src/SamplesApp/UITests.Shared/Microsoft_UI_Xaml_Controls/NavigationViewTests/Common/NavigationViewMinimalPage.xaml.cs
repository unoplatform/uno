// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using MUXControlsTestApp.Utilities;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;

namespace MUXControlsTestApp
{
	[Sample("NavigationView", "MUX")]
	public sealed partial class NavigationViewMinimalPage : TestPage
	{
		public NavigationViewMinimalPage()
		{
			this.InitializeComponent();
		}

		private void GetNavViewActiveVisualStates_Click(object sender, RoutedEventArgs e)
		{
			var visualstates = Utilities.VisualStateHelper.GetCurrentVisualStateName(NavView);
			NavViewActiveVisualStatesResult.Text = string.Join(",", visualstates);
		}
	}
}
