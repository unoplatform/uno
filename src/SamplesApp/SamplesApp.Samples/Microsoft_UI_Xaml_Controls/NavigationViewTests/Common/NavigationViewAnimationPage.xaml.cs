// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using MUXControlsTestApp.Utilities;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using NavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;
using System.Globalization;

namespace MUXControlsTestApp
{
	[Sample("NavigationView", "MUX")]
	public sealed partial class NavigationViewAnimationPage : TestPage
	{
		public NavigationViewAnimationPage()
		{
			this.InitializeComponent();
		}
		private void NavView_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			NavigateToPage(args.InvokedItemContainer.Tag);
		}

		private void NavigateToPage(object pageTag)
		{
			if (pageTag == null)
			{
				pageTag = "BlankPage1";
			}
			var pageName = "MUXControlsTestApp.NavigationView" + pageTag;
			var pageType = GetType(pageName);

			ContentFrame.Navigate(pageType);

			[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Within SamplesApp, `NavigationView*` is treated as [Bindable], and and `Uno.UI.SourceGenerators/BindableTypeProviders` / `BindableMetadata.g.cs` ensures the type exists.")]
			static Type GetType(string typeName)
			{
				return Type.GetType(typeName);
			}
		}

		private void FlipOrientation_Click(object sender, RoutedEventArgs e)
		{
			NavView.PaneDisplayMode = NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top ? NavigationViewPaneDisplayMode.Auto : NavigationViewPaneDisplayMode.Top;
		}
	}
}
