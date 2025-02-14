// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
	using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
	using ElementFactory = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ElementFactory;

	class ElementFromElementElementFactory : ElementFactory
	{
		protected override UIElement GetElementCore(ElementFactoryGetArgs args)
		{
			var button = new Button();
			button.Content = args.Data;
			return button;
		}

		protected override void RecycleElementCore(ElementFactoryRecycleArgs args)
		{
			var container = args.Element as Button;
			container.Content = null;
		}
	}
}
