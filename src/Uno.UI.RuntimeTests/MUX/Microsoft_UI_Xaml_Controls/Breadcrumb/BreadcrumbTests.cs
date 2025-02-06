// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbLayout.cpp, commit 9aedb00

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
#endif

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{

	[TestClass]
	[RequiresFullWindow]
	public class BreadcrumbTests : MUXApiTestBase
	{
		[TestMethod]
		public void VerifyBreadcrumbDefaultAPIValues()
		{
			RunOnUIThread.Execute(() =>
			{
				BreadcrumbBar breadcrumb = new BreadcrumbBar();
				var stackPanel = new StackPanel();
				stackPanel.Children.Add(breadcrumb);

				Content = stackPanel;
				Content.UpdateLayout();

				Verify.IsNull(breadcrumb.ItemsSource, "The default ItemsSource property value must be null");
				Verify.IsNull(breadcrumb.ItemTemplate, "The default ItemTemplate property value must be null");
			});
		}

		[TestMethod]
		public async Task VerifyDefaultBreadcrumb()
		{
			BreadcrumbBar breadcrumb = null;
			await RunOnUIThread.ExecuteAsync(() =>
			{
				breadcrumb = new BreadcrumbBar();
				var stackPanel = new StackPanel();
				stackPanel.Children.Add(breadcrumb);

				Content = stackPanel;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				ItemsRepeater breadcrumbItemsRepeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				Verify.IsNotNull(breadcrumbItemsRepeater, "The underlying items repeater could not be retrieved");

				var breadcrumbNode1 = breadcrumbItemsRepeater.TryGetElement(1);
				Verify.IsNull(breadcrumbNode1, "There should be no items.");
			});
		}

		[TestMethod]
		public async Task VerifyCustomItemTemplate()
		{
			BreadcrumbBar breadcrumb = null;
			BreadcrumbBar breadcrumb2 = null;
			await RunOnUIThread.ExecuteAsync(() =>
			{
				breadcrumb = new BreadcrumbBar();
				breadcrumb.ItemsSource = new List<string>() { "Node 1", "Node 2" };

				// Set a custom ItemTemplate to be wrapped in a BreadcrumbBarItem.
				var itemTemplate = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='{Binding}'/>
                        </DataTemplate>");

				breadcrumb.ItemTemplate = itemTemplate;

				breadcrumb2 = new BreadcrumbBar();
				breadcrumb2.ItemsSource = new List<string>() { "Node 1", "Node 2" };

				// Set a custom ItemTemplate which is already a BreadcrumbBarItem. No wrapping should be performed.
				var itemTemplate2 = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                            xmlns:controls='using:Microsoft" + /* UWP don't rename */ @".UI.Xaml.Controls'>
                            <controls:BreadcrumbBarItem Foreground='Blue'>
                              <TextBlock Text = '{Binding}'/>
                            </controls:BreadcrumbBarItem>
                        </DataTemplate>");

				breadcrumb2.ItemTemplate = itemTemplate2;

				var stackPanel = new StackPanel();
				stackPanel.Children.Add(breadcrumb);
				stackPanel.Children.Add(breadcrumb2);

				Content = stackPanel;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				ItemsRepeater breadcrumbItemsRepeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				ItemsRepeater breadcrumbItemsRepeater2 = (ItemsRepeater)breadcrumb2.FindVisualChildByName("PART_ItemsRepeater");
				Verify.IsNotNull(breadcrumbItemsRepeater, "The underlying items repeater could not be retrieved");
				Verify.IsNotNull(breadcrumbItemsRepeater2, "The underlying items repeater could not be retrieved");

				var breadcrumbNode1 = breadcrumbItemsRepeater.TryGetElement(1) as BreadcrumbBarItem;
				var breadcrumbNode2 = breadcrumbItemsRepeater2.TryGetElement(1) as BreadcrumbBarItem;
				Verify.IsNotNull(breadcrumbNode1, "Our custom ItemTemplate should have been wrapped in a BreadcrumbBarItem.");
				Verify.IsNotNull(breadcrumbNode2, "Our custom ItemTemplate should have been wrapped in a BreadcrumbBarItem.");

				// change this conditions
				bool testCondition = !(breadcrumbNode1.Foreground is SolidColorBrush brush && brush.Color == Colors.Blue);
				Verify.IsTrue(testCondition, "Default foreground color of the BreadcrumbBarItem should not have been [blue].");

				testCondition = breadcrumbNode2.Foreground is SolidColorBrush brush2 && brush2.Color == Colors.Blue;
				Verify.IsTrue(testCondition, "The foreground color of the BreadcrumbBarItem should have been [blue].");
			});
		}

		[TestMethod]
		public async Task VerifyNumericItemsSource()
		{
			BreadcrumbBar breadcrumb = null;
			BreadcrumbBar breadcrumb2 = null;
			await RunOnUIThread.ExecuteAsync(() =>
			{
				// Set a custom ItemTemplate to be wrapped in a BreadcrumbBarItem.
				var itemTemplate = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='{Binding}'/>
                        </DataTemplate>");

				breadcrumb = new BreadcrumbBar();
				breadcrumb.ItemsSource = new List<int>() { 1, 2 };
				breadcrumb.ItemTemplate = itemTemplate;

				breadcrumb2 = new BreadcrumbBar();
				breadcrumb2.ItemsSource = new List<float>() { 1.4f, 4.5f };
				breadcrumb2.ItemTemplate = itemTemplate;

				var stackPanel = new StackPanel();
				stackPanel.Children.Add(breadcrumb);
				stackPanel.Children.Add(breadcrumb2);

				Content = stackPanel;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				ItemsRepeater breadcrumbItemsRepeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				Verify.IsNotNull(breadcrumbItemsRepeater, "The underlying items repeater could not be retrieved");

				ItemsRepeater breadcrumbItemsRepeater2 = (ItemsRepeater)breadcrumb2.FindVisualChildByName("PART_ItemsRepeater");
				Verify.IsNotNull(breadcrumbItemsRepeater2, "The underlying items repeater could not be retrieved");

				var breadcrumbNode1 = breadcrumbItemsRepeater.TryGetElement(1) as BreadcrumbBarItem;
				var breadcrumbNode2 = breadcrumbItemsRepeater2.TryGetElement(1) as BreadcrumbBarItem;
				Verify.IsNotNull(breadcrumbNode1, "Our custom ItemTemplate should have been wrapped in a BreadcrumbBarItem.");
				Verify.IsNotNull(breadcrumbNode2, "Our custom ItemTemplate should have been wrapped in a BreadcrumbBarItem.");
			});
		}

		class MockClass
		{
			public string MockProperty { get; set; }

			public MockClass()
			{
			}
		};

		[TestMethod]
		public async Task VerifyObjectItemsSource()
		{
			BreadcrumbBar breadcrumb = null;
			await RunOnUIThread.ExecuteAsync(() =>
			{
				// Set a custom ItemTemplate to be wrapped in a BreadcrumbBarItem.
				var itemTemplate = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                            xmlns:controls='using:Microsoft" + /* UWP don't rename */ @".UI.Xaml.Controls'
                            xmlns:local='using:Windows.UI.Xaml.Tests.MUXControls.ApiTests'>
                            <controls:BreadcrumbBarItem Content='{Binding}'>
                                <controls:BreadcrumbBarItem.ContentTemplate>
                                    <DataTemplate>
                                        <TextBlock Text='{Binding MockProperty}'/>
                                    </DataTemplate>
                                </controls:BreadcrumbBarItem.ContentTemplate>
                            </controls:BreadcrumbBarItem>
                        </DataTemplate>");


				breadcrumb = new BreadcrumbBar();
				breadcrumb.ItemsSource = new List<MockClass>() {
					new MockClass { MockProperty = "Node 1" },
					new MockClass { MockProperty = "Node 2" },
				};
				breadcrumb.ItemTemplate = itemTemplate;

				var stackPanel = new StackPanel();
				stackPanel.Children.Add(breadcrumb);

				Content = stackPanel;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				ItemsRepeater breadcrumbItemsRepeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				Verify.IsNotNull(breadcrumbItemsRepeater, "The underlying items repeater could not be retrieved");

				var breadcrumbNode1 = breadcrumbItemsRepeater.TryGetElement(1) as BreadcrumbBarItem;
				Verify.IsNotNull(breadcrumbNode1, "Our custom ItemTemplate should have been wrapped in a BreadcrumbBarItem.");
			});
		}

		[TestMethod]
		public async Task VerifyDropdownItemTemplate()
		{
			try
			{
				BreadcrumbBar breadcrumb = null;

				await RunOnUIThread.ExecuteAsync(() =>
				{
					breadcrumb = new BreadcrumbBar();
					breadcrumb.ItemsSource = new List<MockClass>() {
					new MockClass { MockProperty = "Node 1" },
					new MockClass { MockProperty = "Node 2" },
					};

					// Set a custom ItemTemplate to be wrapped in a BreadcrumbBarItem.
					var itemTemplate = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                            xmlns:controls='using:Microsoft" + /* UWP don't rename */ @".UI.Xaml.Controls'
                            xmlns:local='using:Windows.UI.Xaml.Tests.MUXControls.ApiTests'>
                            <controls:BreadcrumbBarItem Content='{Binding}'>
                                <controls:BreadcrumbBarItem.ContentTemplate>
                                    <DataTemplate>
                                        <TextBlock Text='{Binding MockProperty}'/>
                                    </DataTemplate>
                                </controls:BreadcrumbBarItem.ContentTemplate>
                            </controls:BreadcrumbBarItem>
                        </DataTemplate>");

					breadcrumb.ItemTemplate = itemTemplate;

					var stackPanel = new StackPanel();
					stackPanel.Width = 60;
					stackPanel.Children.Add(breadcrumb);

					Content = stackPanel;
					Content.UpdateLayout();
				});

				await TestServices.WindowHelper.WaitForIdle();

				Button ellipsisButton = null;
				await RunOnUIThread.ExecuteAsync(() =>
				{
					ItemsRepeater breadcrumbItemsRepeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
					Verify.IsNotNull(breadcrumbItemsRepeater, "The underlying items repeater (1) could not be retrieved");

					var breadcrumbNode1 = breadcrumbItemsRepeater.TryGetElement(0) as BreadcrumbBarItem;
					Verify.IsNotNull(breadcrumbNode1, "Our custom ItemTemplate (1) should have been wrapped in a BreadcrumbBarItem.");

					ellipsisButton = (Button)breadcrumbNode1.FindVisualChildByName("PART_ItemButton");
					Verify.IsNotNull(ellipsisButton, "The ellipsis item (1) could not be retrieved");

					var automationPeer = new ButtonAutomationPeer(ellipsisButton);
					var invokationPattern = automationPeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
					invokationPattern?.Invoke();
				});

				// GetOpenPopups returns empty list in RS3. The scenario works, this just seems to be a
				// test/infra issue in RS3, so filtering it out for now.
				if (PlatformConfiguration.IsOsVersionGreaterThan(OSVersion.Redstone3))
				{
					await TestServices.WindowHelper.WaitForIdle();

					await RunOnUIThread.ExecuteAsync(() =>
					{
						var openPopups = VisualTreeHelper.GetOpenPopupsForXamlRoot(Content.XamlRoot);
						var flyout = openPopups[openPopups.Count - 1];
						Verify.IsNotNull(flyout, "Flyout could not be retrieved");
						var ellipsisItemsRepeater = TestUtilities.FindDescendents<ItemsRepeater>(flyout).Single();
						Verify.IsNotNull(ellipsisItemsRepeater, "The underlying flyout items repeater (1) could not be retrieved");

						ellipsisItemsRepeater.Loaded += (object sender, RoutedEventArgs e) =>
						{
							TextBlock ellipsisNode1 = ellipsisItemsRepeater.TryGetElement(0) as TextBlock;
							Verify.IsNotNull(ellipsisNode1, "Our flyout ItemTemplate (1) should have been wrapped in a TextBlock.");

							// change this conditions
							bool testCondition = !(ellipsisNode1.Foreground is SolidColorBrush brush && brush.Color == Colors.Blue);
							Verify.IsTrue(testCondition, "Default foreground color of the BreadcrumbBarItem should not have been [blue].");
						};
					});
				}
			}
			finally
			{
#if HAS_UNO
				await RunOnUIThread.ExecuteAsync(() =>
				{
					VisualTreeHelper.CloseAllFlyouts(TestServices.WindowHelper.XamlRoot);
				});
#endif
			}
		}

		[TestMethod]
		public async Task VerifyDropdownItemTemplateWithNoControl()
		{
			try
			{
				BreadcrumbBar breadcrumb = null;

				await RunOnUIThread.ExecuteAsync(() =>
				{
					breadcrumb = new BreadcrumbBar();
					breadcrumb.ItemsSource = new List<string>() { "Node 1", "Node 2" };

					// Set a custom ItemTemplate to be wrapped in a BreadcrumbBarItem.
					var itemTemplate = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='{Binding}'/>
                        </DataTemplate>");

					breadcrumb.ItemTemplate = itemTemplate;

					var stackPanel = new StackPanel();
					stackPanel.Width = 60;
					stackPanel.Children.Add(breadcrumb);

					Content = stackPanel;
					Content.UpdateLayout();
				});

				await TestServices.WindowHelper.WaitForIdle();

				Button ellipsisButton = null;
				await RunOnUIThread.ExecuteAsync(() =>
				{
					ItemsRepeater breadcrumbItemsRepeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
					Verify.IsNotNull(breadcrumbItemsRepeater, "The underlying items repeater (1) could not be retrieved");

					var breadcrumbNode1 = breadcrumbItemsRepeater.TryGetElement(0) as BreadcrumbBarItem;
					Verify.IsNotNull(breadcrumbNode1, "Our custom ItemTemplate (1) should have been wrapped in a BreadcrumbBarItem.");

					ellipsisButton = (Button)breadcrumbNode1.FindVisualChildByName("PART_ItemButton");
					Verify.IsNotNull(ellipsisButton, "The ellipsis item (1) could not be retrieved");

					var automationPeer = new ButtonAutomationPeer(ellipsisButton);
					var invokationPattern = automationPeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
					invokationPattern?.Invoke();
				});

				// GetOpenPopups returns empty list in RS3. The scenario works, this just seems to be a
				// test/infra issue in RS3, so filtering it out for now.
				if (PlatformConfiguration.IsOsVersionGreaterThan(OSVersion.Redstone3))
				{
					await TestServices.WindowHelper.WaitForIdle();

					await RunOnUIThread.ExecuteAsync(() =>
					{
						var openPopups = VisualTreeHelper.GetOpenPopupsForXamlRoot(Content.XamlRoot);
						var flyout = openPopups[openPopups.Count - 1];
						Verify.IsNotNull(flyout, "Flyout could not be retrieved");
						var ellipsisItemsRepeater = TestUtilities.FindDescendents<ItemsRepeater>(flyout).Single();
						Verify.IsNotNull(ellipsisItemsRepeater, "The underlying flyout items repeater (1) could not be retrieved");

						ellipsisItemsRepeater.Loaded += (object sender, RoutedEventArgs e) =>
						{
							TextBlock ellipsisNode1 = ellipsisItemsRepeater.TryGetElement(0) as TextBlock;
							Verify.IsNotNull(ellipsisNode1, "Our flyout ItemTemplate (1) should have been wrapped in a TextBlock.");

							// change this conditions
							bool testCondition = !(ellipsisNode1.Foreground is SolidColorBrush brush && brush.Color == Colors.Blue);
							Verify.IsTrue(testCondition, "Default foreground color of the BreadcrumbBarItem should not have been [blue].");
						};
					});
				}
			}
			finally
			{
#if HAS_UNO
				await RunOnUIThread.ExecuteAsync(() =>
				{
					VisualTreeHelper.CloseAllFlyouts(TestServices.WindowHelper.XamlRoot);
				});
#endif
			}
		}

		[TestMethod]
		public async Task VerifyCollectionChangeGetsRespected()
		{
			BreadcrumbBar breadcrumb = null;
			ItemsRepeater breadcrumbItemsRepeater = null;
			await RunOnUIThread.ExecuteAsync(() =>
			{
				// Set a custom ItemTemplate to be wrapped in a BreadcrumbBarItem.
				var itemTemplate = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                            xmlns:controls='using:Microsoft" + /* UWP don't rename */ @".UI.Xaml.Controls'
                            xmlns:local='using:Windows.UI.Xaml.Tests.MUXControls.ApiTests'>
                            <controls:BreadcrumbBarItem Content='{Binding}'>
                                <controls:BreadcrumbBarItem.ContentTemplate>
                                    <DataTemplate>
                                        <TextBlock Text='{Binding MockProperty}'/>
                                    </DataTemplate>
                                </controls:BreadcrumbBarItem.ContentTemplate>
                            </controls:BreadcrumbBarItem>
                        </DataTemplate>");


				breadcrumb = new BreadcrumbBar();
				breadcrumb.ItemsSource = new List<MockClass>() {
					new MockClass { MockProperty = "Node 1" },
					new MockClass { MockProperty = "Node 2" },
				};
				breadcrumb.ItemTemplate = itemTemplate;

				var stackPanel = new StackPanel();
				stackPanel.Children.Add(breadcrumb);

				Content = stackPanel;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				breadcrumbItemsRepeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				Verify.IsNotNull(breadcrumbItemsRepeater, "The underlying items repeater could not be retrieved");

				var breadcrumbNode2 = breadcrumbItemsRepeater.TryGetElement(1) as BreadcrumbBarItem;
				Verify.IsNotNull(breadcrumbNode2, "Our custom ItemTemplate should have been wrapped in a BreadcrumbBarItem.");

				breadcrumb.ItemsSource = new List<MockClass>() {
					new MockClass { MockProperty = "Node 1" },
					new MockClass { MockProperty = "Node 2" },
					new MockClass { MockProperty = "Node 3" },
					new MockClass { MockProperty = "Node 4" },
				};
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var breadcrumbNode3 = breadcrumbItemsRepeater.TryGetElement(3) as BreadcrumbBarItem;
				Verify.IsNotNull(breadcrumbNode3, "A fourth item should have been rendered by the BreadcrumControl");
			});
		}
	}
}
