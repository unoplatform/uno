﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common;
using Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common.Mocks;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using VirtualizingLayout = Microsoft.UI.Xaml.Controls.VirtualizingLayout;
using ItemsRepeater = Microsoft.UI.Xaml.Controls.ItemsRepeater;
using ItemsSourceView = Microsoft.UI.Xaml.Controls.ItemsSourceView;
using ElementFactory = Microsoft.UI.Xaml.Controls.ElementFactory;
using VirtualizingLayoutContext = Microsoft.UI.Xaml.Controls.VirtualizingLayoutContext;
using RepeaterAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.RepeaterAutomationPeer;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
	[TestClass]
	public class AccessibilityTests : MUXApiTestBase
	{
		[TestMethod]
		[Ignore("Automation peers are not fully supported on Uno https://github.com/unoplatform/uno/issues/4529")]
		public void ValidateChildrenPeers()
		{
			RunOnUIThread.Execute(() =>
			{
				var data = new ObservableCollection<string>(Enumerable.Range(0, 3).Select(i => string.Format("Item #{0}", i)));
				var dataSource = MockItemsSource.CreateDataSource(data, supportsUniqueIds: false);

				// StackPanel doesn't have an automation peer, but we still need
				// to report its children's peer.
				var group = new StackPanel();
				group.Children.Add(new ListViewItem());
				group.Children.Add(new ListViewItem());

				var mapping = new Dictionary<int, UIElement>
				{
					{ 0, new ListViewItem() },
					{ 1, new ListViewItem() },
					{ 2, group },
				};

				var elementFactory = MockElementFactory.CreateElementFactory(mapping);
				var layout = new MockVirtualizingLayout
				{
					MeasureLayoutFunc = (availableSize, context) =>
					{
						var ctx = (VirtualizingLayoutContext)context;

						// Realize elements 0, 1, 2 in a random order.
						ctx.GetOrCreateElementAt(2);
						ctx.GetOrCreateElementAt(0);
						var element1 = ctx.GetOrCreateElementAt(1);

						// Clear element 1 because we will be validating
						// that we ignore unrealized elements.
						ctx.RecycleElement(element1);

						return default(Size);
					}
				};

				var repeater = CreateRepeater(dataSource, elementFactory, layout);

				Content = repeater;
				repeater.UpdateLayout();

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(repeater);
				var children = peer.GetChildren().Select(p => ((FrameworkElementAutomationPeer)p).Owner).ToList();

				Verify.AreEqual(3, children.Count);
				Verify.AreEqual(mapping[0], children[0]);
				Verify.AreEqual(group.Children[0], children[1]);
				Verify.AreEqual(group.Children[1], children[2]);
			});
		}

		private ItemsRepeater CreateRepeater(object dataSource, object elementFactory, VirtualizingLayout layout = null)
		{
			var repeater = new ItemsRepeater
			{
				ItemsSource = dataSource,
				ItemTemplate = elementFactory,
			};
			repeater.Layout = layout ?? CreateLayout(repeater);
			return repeater;
		}

		private VirtualizingLayout CreateLayout(ItemsRepeater repeater)
		{
			var layout = new MockVirtualizingLayout();
			var children = new List<UIElement>();

			layout.MeasureLayoutFunc = (availableSize, context) =>
			{
				repeater.Tag = repeater.Tag ?? context;
				children.Clear();
				var itemCount = context.ItemCount;

				for (int i = 0; i < itemCount; ++i)
				{
					var element = repeater.TryGetElement(i) ?? context.GetOrCreateElementAt(i);
					element.Measure(availableSize);
					children.Add(element);
				}

				return new Size(10, 10);
			};

			return layout;
		}
	}
}
