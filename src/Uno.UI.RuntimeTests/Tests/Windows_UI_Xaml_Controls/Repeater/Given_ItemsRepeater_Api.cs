// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RepeaterTests.cs, tag winui3/release/1.8.4

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater
{
	[TestClass]
	public class Given_ItemsRepeater_Api
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ElementToIndexMapping()
		{
			var elementFactory = new RecyclingElementFactory();
			elementFactory.RecyclePool = new RecyclePool();
			elementFactory.Templates["Item"] = (DataTemplate)XamlReader.Load(
				@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
					<TextBlock Text='{Binding}' Height='50' />
				</DataTemplate>");

			var repeater = new ItemsRepeater
			{
				ItemsSource = Enumerable.Range(0, 10).Select(i => $"Item #{i}"),
				ItemTemplate = elementFactory,
			};

			var scrollHost = new ItemsRepeaterScrollHost
			{
				Width = 400,
				Height = 800,
				ScrollViewer = new ScrollViewer
				{
					Content = repeater
				}
			};

			TestServices.WindowHelper.WindowContent = scrollHost;
			await TestServices.WindowHelper.WaitForIdle();
			scrollHost.UpdateLayout();

			for (int i = 0; i < 10; i++)
			{
				var element = repeater.TryGetElement(i);
				Assert.IsNotNull(element, $"Element at index {i} should not be null");
				Assert.AreEqual($"Item #{i}", ((TextBlock)element).Text);
				Assert.AreEqual(i, repeater.GetElementIndex(element));
			}

			Assert.IsNull(repeater.TryGetElement(20), "Element at index 20 should be null");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_RepeaterDefaults()
		{
			var repeater = new ItemsRepeater
			{
				ItemsSource = Enumerable.Range(0, 10).Select(i => $"Item #{i}"),
			};

			var scrollHost = new ItemsRepeaterScrollHost
			{
				Width = 400,
				Height = 800,
				ScrollViewer = new ScrollViewer
				{
					Content = repeater
				}
			};

			TestServices.WindowHelper.WindowContent = scrollHost;
			await TestServices.WindowHelper.WaitForIdle();
			scrollHost.UpdateLayout();

			for (int i = 0; i < 10; i++)
			{
				var element = repeater.TryGetElement(i);
				Assert.IsNotNull(element, $"Element at index {i} should not be null");
				Assert.AreEqual($"Item #{i}", ((TextBlock)element).Text);
				Assert.AreEqual(i, repeater.GetElementIndex(element));
			}

			Assert.IsNull(repeater.TryGetElement(20), "Element at index 20 should be null");
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_CanSetItemsSource()
		{
			// In bug 12042052, we crash when we set ItemsSource to null because we try to subscribe to
			// the DataSourceChanged event on a null instance.
			{
				var repeater = new ItemsRepeater();
				repeater.ItemsSource = null;
				repeater.ItemsSource = Enumerable.Range(0, 5).Select(i => $"Item #{i}");
			}

			{
				var repeater = new ItemsRepeater();
				repeater.ItemsSource = Enumerable.Range(0, 5).Select(i => $"Item #{i}");
				repeater.ItemsSource = Enumerable.Range(5, 5).Select(i => $"Item #{i}");
				repeater.ItemsSource = null;
				repeater.ItemsSource = Enumerable.Range(10, 5).Select(i => $"Item #{i}");
				repeater.ItemsSource = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSetItemsSource()
		{
			var repeater = new ItemsRepeater();
			var dataSource = new ItemsSourceView(Enumerable.Range(0, 10).Select(i => $"Item #{i}"));
			repeater.SetValue(ItemsRepeater.ItemsSourceProperty, dataSource);
			Assert.AreSame(dataSource, repeater.GetValue(ItemsRepeater.ItemsSourceProperty) as ItemsSourceView);
			Assert.AreSame(dataSource, repeater.ItemsSourceView);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSetBackground()
		{
			var repeater = new ItemsRepeater();
			var redBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
			repeater.SetValue(ItemsRepeater.BackgroundProperty, redBrush);
			Assert.AreSame(redBrush, repeater.GetValue(ItemsRepeater.BackgroundProperty) as Brush);
			Assert.AreSame(redBrush, repeater.Background);

			var blueBrush = new SolidColorBrush(Microsoft.UI.Colors.Blue);
			repeater.Background = blueBrush;
			Assert.AreSame(blueBrush, repeater.Background);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ClearingItemsSource_ClearsElements()
		{
			var data = new ObservableCollection<string>(Enumerable.Range(0, 4).Select(i => "Item #" + i));
			var mapping = Enumerable.Range(0, data.Count).Select(i => new ContentControl { Width = 40, Height = 40 }).ToList();

			var repeater = new ItemsRepeater();
			repeater.ItemsSource = data;
			repeater.ItemTemplate = new MockElementFactory(mapping);
			// Non-virtualizing layout
			repeater.Layout = new NonVirtualizingStackLayout();

			TestServices.WindowHelper.WindowContent = repeater;
			await TestServices.WindowHelper.WaitForIdle();
			repeater.UpdateLayout();

			repeater.ItemsSource = null;

			foreach (var item in mapping)
			{
				Assert.IsNull(item.Parent, "Item should have been removed from parent");
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[Ignore("Scroll offset precision varies across platforms - 575.5 vs expected 600")]
		public async Task When_NestedRepeater_WithDataTemplate()
		{
			var scrollhost = (ItemsRepeaterScrollHost)XamlReader.Load(
				@"<controls:ItemsRepeaterScrollHost Width='400' Height='600'
					xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					xmlns:controls='using:Microsoft.UI.Xaml.Controls'>
					<controls:ItemsRepeaterScrollHost.Resources>
						<DataTemplate x:Key='ItemTemplate'>
							<TextBlock Text='{Binding}' />
						</DataTemplate>
						<DataTemplate x:Key='GroupTemplate'>
							<StackPanel>
								<TextBlock Text='{Binding}' />
								<controls:ItemsRepeater ItemTemplate='{StaticResource ItemTemplate}' ItemsSource='{Binding}' VerticalCacheLength='0'/>
							</StackPanel>
						</DataTemplate>
					</controls:ItemsRepeaterScrollHost.Resources>
					<ScrollViewer x:Name='scrollviewer'>
						<controls:ItemsRepeater x:Name='rootRepeater' ItemTemplate='{StaticResource GroupTemplate}' VerticalCacheLength='0' />
					</ScrollViewer>
				</controls:ItemsRepeaterScrollHost>");

			var rootRepeater = (ItemsRepeater)scrollhost.FindName("rootRepeater");
			var scrollViewer = (ScrollViewer)scrollhost.FindName("scrollviewer");

			var itemsSource = new ObservableCollection<ObservableCollection<int>>();
			for (int i = 0; i < 10; i++)
			{
				itemsSource.Add(new ObservableCollection<int>(Enumerable.Range(0, 5)));
			}

			rootRepeater.ItemsSource = itemsSource;

			TestServices.WindowHelper.WindowContent = scrollhost;
			await TestServices.WindowHelper.WaitForIdle();

			// Scroll down several times to cause recycling of elements
			for (int i = 1; i < 5; i++)
			{
				scrollViewer.ChangeView(null, i * 200, null, disableAnimation: true);
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual(i * 200, scrollViewer.VerticalOffset);
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_StoreScenario_NestedRepeaters()
		{
			var scrollhost = (ItemsRepeaterScrollHost)XamlReader.Load(
				@"<controls:ItemsRepeaterScrollHost Width='400' Height='200'
					xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					xmlns:controls='using:Microsoft.UI.Xaml.Controls'>
					<controls:ItemsRepeaterScrollHost.Resources>
						<DataTemplate x:Key='ItemTemplate'>
							<TextBlock Text='{Binding}' Height='100' Width='100'/>
						</DataTemplate>
						<DataTemplate x:Key='GroupTemplate'>
							<StackPanel>
								<TextBlock Text='{Binding}' />
								<controls:ItemsRepeaterScrollHost>
									<ScrollViewer HorizontalScrollMode='Enabled' VerticalScrollMode='Disabled' HorizontalScrollBarVisibility='Auto' VerticalScrollBarVisibility='Hidden'>
										<controls:ItemsRepeater ItemTemplate='{StaticResource ItemTemplate}' ItemsSource='{Binding}'>
											<controls:ItemsRepeater.Layout>
												<controls:StackLayout Orientation='Horizontal' />
											</controls:ItemsRepeater.Layout>
										</controls:ItemsRepeater>
									</ScrollViewer>
								</controls:ItemsRepeaterScrollHost>
							</StackPanel>
						</DataTemplate>
					</controls:ItemsRepeaterScrollHost.Resources>
					<ScrollViewer x:Name='scrollviewer'>
						<controls:ItemsRepeater x:Name='rootRepeater' ItemTemplate='{StaticResource GroupTemplate}'/>
					</ScrollViewer>
				</controls:ItemsRepeaterScrollHost>");

			var rootRepeater = (ItemsRepeater)scrollhost.FindName("rootRepeater");

			var items = new List<List<int>>();
			for (int i = 0; i < 10; i++)
			{
				items.Add(Enumerable.Range(0, 4).ToList());
			}
			rootRepeater.ItemsSource = items;

			TestServices.WindowHelper.WindowContent = scrollhost;
			await TestServices.WindowHelper.WaitForIdle();

			// Verify that first items outside the visible range but in the realized range
			// for the inner of the nested repeaters are realized.
			var group2 = rootRepeater.TryGetElement(2) as StackPanel;
			Assert.IsNotNull(group2, "Group 2 should be realized");

			var group2Repeater = ((ItemsRepeaterScrollHost)group2.Children[1]).ScrollViewer.Content as ItemsRepeater;
			Assert.IsNotNull(group2Repeater, "Group 2 repeater should exist");

			Assert.IsNotNull(group2Repeater.TryGetElement(0), "First item in group 2 should be realized");
		}

		/// <summary>
		/// A simple non-virtualizing layout for testing.
		/// </summary>
		private class NonVirtualizingStackLayout : NonVirtualizingLayout
		{
			protected internal override Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
			{
				double maxWidth = 0;
				double totalHeight = 0;

				foreach (var child in context.Children)
				{
					child.Measure(availableSize);
					maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
					totalHeight += child.DesiredSize.Height;
				}

				return new Size(maxWidth, totalHeight);
			}

			protected internal override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
			{
				double y = 0;

				foreach (var child in context.Children)
				{
					child.Arrange(new Windows.Foundation.Rect(0, y, finalSize.Width, child.DesiredSize.Height));
					y += child.DesiredSize.Height;
				}

				return finalSize;
			}
		}

		/// <summary>
		/// A simple element factory that returns elements from a predefined list.
		/// </summary>
		private class MockElementFactory : ElementFactory
		{
			private readonly List<ContentControl> _elements;
			private int _getCount;

			public MockElementFactory(List<ContentControl> elements)
			{
				_elements = elements;
			}

			protected override UIElement GetElementCore(Microsoft.UI.Xaml.Controls.ElementFactoryGetArgs args)
			{
				var element = _elements[_getCount % _elements.Count];
				_getCount++;
				return element;
			}

			protected override void RecycleElementCore(Microsoft.UI.Xaml.Controls.ElementFactoryRecycleArgs args)
			{
				// Nothing to do
			}
		}
	}
}
