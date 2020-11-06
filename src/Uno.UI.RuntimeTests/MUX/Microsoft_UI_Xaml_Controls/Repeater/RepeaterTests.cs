// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MUXControlsTestApp.Utilities;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Common;
using Windows.UI.Xaml.Media;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using ItemsRepeater = Microsoft.UI.Xaml.Controls.ItemsRepeater;
using Layout = Microsoft.UI.Xaml.Controls.Layout;
using ItemsSourceView = Microsoft.UI.Xaml.Controls.ItemsSourceView;
using RecyclingElementFactory = Microsoft.UI.Xaml.Controls.RecyclingElementFactory;
using RecyclePool = Microsoft.UI.Xaml.Controls.RecyclePool;
using StackLayout = Microsoft.UI.Xaml.Controls.StackLayout;
using ItemsRepeaterScrollHost = Microsoft.UI.Xaml.Controls.ItemsRepeaterScrollHost;
using System.Collections.ObjectModel;
using System.Threading;
using System.Collections.Generic;
using Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common.Mocks;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
    [TestClass]
    public class RepeaterTests : ApiTestBase
    {
        [TestMethod]
        public void ValidateElementToIndexMapping()
        {
            ItemsRepeater repeater = null;
            RunOnUIThread.Execute(() =>
            {
                var elementFactory = new RecyclingElementFactory();
                elementFactory.RecyclePool = new RecyclePool();
                elementFactory.Templates["Item"] = (DataTemplate)XamlReader.Load(
                    @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> 
                          <TextBlock Text='{Binding}' Height='50' />
                      </DataTemplate>");

                repeater = new ItemsRepeater() {
                    ItemsSource = Enumerable.Range(0, 10).Select(i => string.Format("Item #{0}", i)),
                    ItemTemplate = elementFactory,
                    // Default is StackLayout, so do not have to explicitly set.
                    // Layout = new StackLayout(),
                };

                Content = new ItemsRepeaterScrollHost() {
                    Width = 400,
                    Height = 800,
                    ScrollViewer = new ScrollViewer {
                        Content = repeater
                    }
                };

                Content.UpdateLayout();

                for (int i = 0; i < 10; i++)
                {
                    var element = repeater.TryGetElement(i);
                    Verify.IsNotNull(element);
                    Verify.AreEqual(string.Format("Item #{0}", i), ((TextBlock)element).Text);
                    Verify.AreEqual(i, repeater.GetElementIndex(element));
                }

                Verify.IsNull(repeater.TryGetElement(20));
            });
        }

        [TestMethod]
        public void ValidateRepeaterDefaults()
        {
            RunOnUIThread.Execute(() =>
            {
                var repeater = new ItemsRepeater() {
                    ItemsSource = Enumerable.Range(0, 10).Select(i => string.Format("Item #{0}", i)),
                };

                Content = new ItemsRepeaterScrollHost() {
                    Width = 400,
                    Height = 800,
                    ScrollViewer = new ScrollViewer {
                        Content = repeater
                    }
                };

                Content.UpdateLayout();

                for (int i = 0; i < 10; i++)
                {
                    var element = repeater.TryGetElement(i);
                    Verify.IsNotNull(element);
                    Verify.AreEqual(string.Format("Item #{0}", i), ((TextBlock)element).Text);
                    Verify.AreEqual(i, repeater.GetElementIndex(element));
                }

                Verify.IsNull(repeater.TryGetElement(20));
            });
        }

        [TestMethod]
        [TestProperty("Bug", "12042052")]
        public void CanSetItemsSource()
        {
            // In bug 12042052, we crash when we set ItemsSource to null because we try to subscribe to
            // the DataSourceChanged event on a null instance.
            RunOnUIThread.Execute(() =>
            {
                {
                    var repeater = new ItemsRepeater();
                    repeater.ItemsSource = null;
                    repeater.ItemsSource = Enumerable.Range(0, 5).Select(i => string.Format("Item #{0}", i));
                }

                {
                    var repeater = new ItemsRepeater();
                    repeater.ItemsSource = Enumerable.Range(0, 5).Select(i => string.Format("Item #{0}", i));
                    repeater.ItemsSource = Enumerable.Range(5, 5).Select(i => string.Format("Item #{0}", i));
                    repeater.ItemsSource = null;
                    repeater.ItemsSource = Enumerable.Range(10, 5).Select(i => string.Format("Item #{0}", i));
                    repeater.ItemsSource = null;
                }
            });
        }

        [TestMethod]
        public void ValidateGetSetItemsSource()
        {
            RunOnUIThread.Execute(() =>
            {
                ItemsRepeater repeater = new ItemsRepeater();
                var dataSource = new ItemsSourceView(Enumerable.Range(0, 10).Select(i => string.Format("Item #{0}", i)));
                repeater.SetValue(ItemsRepeater.ItemsSourceProperty, dataSource);
                Verify.AreSame(dataSource, repeater.GetValue(ItemsRepeater.ItemsSourceProperty) as ItemsSourceView);
                Verify.AreSame(dataSource, repeater.ItemsSourceView);
            });
        }

        [TestMethod]
        public void VerifyClearingItemsSourceClearsElements()
        {
            var data = new ObservableCollection<string>(Enumerable.Range(0, 4).Select(i => "Item #" + i));
            var mapping = (List<ContentControl>)null;

            RunOnUIThread.Execute(() =>
            {
                mapping = Enumerable.Range(0, data.Count).Select(i => new ContentControl { Width = 40, Height = 40 }).ToList();

                var dataSource = MockItemsSource.CreateDataSource(data, supportsUniqueIds: false);
                var elementFactory = MockElementFactory.CreateElementFactory(mapping);
                ItemsRepeater repeater = new ItemsRepeater();
                repeater.ItemsSource = dataSource;
                repeater.ItemTemplate = elementFactory;
                // This was an issue only for NonVirtualizing layouts
                repeater.Layout = new MyCustomNonVirtualizingStackLayout();
                Content = repeater;
                Content.UpdateLayout();

                repeater.ItemsSource = null;
            });

            foreach(var item in mapping)
            {
                Verify.IsNull(item.Parent);
            }
        }

        [TestMethod]
        public void ValidateGetSetBackground()
        {
            RunOnUIThread.Execute(() =>
            {
                ItemsRepeater repeater = new ItemsRepeater();
                var redBrush = new SolidColorBrush(Colors.Red);
                repeater.SetValue(ItemsRepeater.BackgroundProperty, redBrush);
                Verify.AreSame(redBrush, repeater.GetValue(ItemsRepeater.BackgroundProperty) as Brush);
                Verify.AreSame(redBrush, repeater.Background);

                var blueBrush = new SolidColorBrush(Colors.Blue);
                repeater.Background = blueBrush;
                Verify.AreSame(blueBrush, repeater.Background);
            });
        }

        [TestMethod]
        public void VerifyCurrentAnchor()
        {
            if (PlatformConfiguration.IsDebugBuildConfiguration())
            {
                // Test is failing in chk configuration due to:
                // Bug #1726 Test Failure: RepeaterTests.VerifyCurrentAnchor 
                Log.Warning("Skipping test for Debug builds.");
                return;
            }

            ItemsRepeater rootRepeater = null;
            ScrollViewer scrollViewer = null;
            ItemsRepeaterScrollHost scrollhost = null;
            ManualResetEvent viewChanged = new ManualResetEvent(false);
            RunOnUIThread.Execute(() =>
            {
                scrollhost = (ItemsRepeaterScrollHost)XamlReader.Load(
                  @"<controls:ItemsRepeaterScrollHost Width='400' Height='600'
                     xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                     xmlns:controls='using:Microsoft.UI.Xaml.Controls'>
                    <controls:ItemsRepeaterScrollHost.Resources>
                        <DataTemplate x:Key='ItemTemplate' >
                            <TextBlock Text='{Binding}' Height='50'/>
                        </DataTemplate>
                    </controls:ItemsRepeaterScrollHost.Resources>
                    <ScrollViewer x:Name='scrollviewer'>
                        <controls:ItemsRepeater x:Name='rootRepeater' ItemTemplate='{StaticResource ItemTemplate}' VerticalCacheLength='0' />
                    </ScrollViewer>
                </controls:ItemsRepeaterScrollHost>");

                rootRepeater = (ItemsRepeater)scrollhost.FindName("rootRepeater");
                scrollViewer = (ScrollViewer)scrollhost.FindName("scrollviewer");
                scrollViewer.ViewChanged += (sender, args) =>
                {
                    if (!args.IsIntermediate)
                    {
                        viewChanged.Set();
                    }
                };

                rootRepeater.ItemsSource = Enumerable.Range(0, 500);
                Content = scrollhost;
            });

            // scroll down several times and validate current anchor
            for (int i = 1; i < 10; i++)
            {
                IdleSynchronizer.Wait();
                RunOnUIThread.Execute(() =>
                {
                    scrollViewer.ChangeView(null, i * 200, null);
                });

                Verify.IsTrue(viewChanged.WaitOne(DefaultWaitTimeInMS));
                viewChanged.Reset();
                IdleSynchronizer.Wait();

                RunOnUIThread.Execute(() =>
                {
                    Verify.AreEqual(i * 200, scrollViewer.VerticalOffset);
                    var anchor = PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone5) ?
                            scrollhost.CurrentAnchor :
                            scrollViewer.CurrentAnchor;
                    var anchorIndex = rootRepeater.GetElementIndex(anchor);
                    Log.Comment("CurrentAnchor: " + anchorIndex);
                    Verify.AreEqual(i * 4, anchorIndex);
                });
            }
        }

        // Ensure that scrolling a nested repeater works when the 
        // Itemtemplates are data templates.
        [TestMethod]
        public void NestedRepeaterWithDataTemplateScenario()
        {
            NestedRepeaterWithDataTemplateScenario(disableAnimation: true);
            NestedRepeaterWithDataTemplateScenario(disableAnimation: false);
        }

        [TestMethod]
        public void VerifyFocusedItemIsRecycledOnCollectionReset()
        {
            List<Layout> layouts = new List<Layout>();
            RunOnUIThread.Execute(() =>
            {
                layouts.Add(new MyCustomNonVirtualizingStackLayout());
                layouts.Add(new StackLayout());
            });

            foreach (var layout in layouts)
            {
                List<string> items = new List<string> { "item0", "item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9" };
                const int targetIndex = 4;
                string targetItem = items[targetIndex];
                ItemsRepeater repeater = null;

                RunOnUIThread.Execute(() =>
                {
                    repeater = new ItemsRepeater() {
                        ItemsSource = items,
                        ItemTemplate = CreateDataTemplateWithContent(@"<Button Content='{Binding}'/>"),
                        Layout = layout
                    };
                    Content = repeater;
                });

                IdleSynchronizer.Wait();

                RunOnUIThread.Execute(() =>
                {
                    Log.Comment("Setting Focus on item " + targetIndex);
                    Button toFocus = (Button)repeater.TryGetElement(targetIndex);
                    Verify.AreEqual(targetItem, toFocus.Content as string);
                    toFocus.Focus(FocusState.Keyboard);
                });

                IdleSynchronizer.Wait();

                RunOnUIThread.Execute(() =>
                {
                    Log.Comment("Removing focused element from collection");
                    items.Remove(targetItem);

                    Log.Comment("Reset the collection with an empty list");
                    repeater.ItemsSource = new List<string>();
                });

                IdleSynchronizer.Wait();

                RunOnUIThread.Execute(() =>
                {
                    Log.Comment("Verify new elements");
                    for (int i = 0; i < items.Count; i++)
                    {
                        Button currentButton = (Button)repeater.TryGetElement(i);
                        Verify.IsNull(currentButton);
                    }
                });
            }
        }

        private DataTemplate CreateDataTemplateWithContent(string content)
        {
            return (DataTemplate)XamlReader.Load(@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" + content + @"</DataTemplate>");
        }

        private void NestedRepeaterWithDataTemplateScenario(bool disableAnimation)
        {
            if (!disableAnimation && PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
            {
                Log.Warning("This test is showing consistent issues with not scrolling enough on RS5 and 19H1 when animations are enabled, tracked by microsoft-ui-xaml#779");
                return;
            }

            // Example of how to include debug tracing in an ApiTests.ItemsRepeater test's output.
            // using (PrivateLoggingHelper privateLoggingHelper = new PrivateLoggingHelper("Repeater"))
            // {
            ItemsRepeater rootRepeater = null;
            ScrollViewer scrollViewer = null;
            ManualResetEvent viewChanged = new ManualResetEvent(false);
            RunOnUIThread.Execute(() =>
            {
                var anchorProvider = (ItemsRepeaterScrollHost)XamlReader.Load(
                    @"<controls:ItemsRepeaterScrollHost Width='400' Height='600'
                        xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                        xmlns:controls='using:Microsoft.UI.Xaml.Controls'>
                    <controls:ItemsRepeaterScrollHost.Resources>
                        <DataTemplate x:Key='ItemTemplate' >
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

                rootRepeater = (ItemsRepeater)anchorProvider.FindName("rootRepeater");
                rootRepeater.SizeChanged += (sender, args) =>
                {
                    Log.Comment($"SizeChanged: Size=({rootRepeater.ActualWidth} x {rootRepeater.ActualHeight})");
                };

                scrollViewer = (ScrollViewer)anchorProvider.FindName("scrollviewer");
                scrollViewer.ViewChanging += (sender, args) =>
                {
                    Log.Comment($"ViewChanging: Next VerticalOffset={args.NextView.VerticalOffset}, Final VerticalOffset={args.FinalView.VerticalOffset}");
                };
                scrollViewer.ViewChanged += (sender, args) =>
                {
                    Log.Comment($"ViewChanged: VerticalOffset={scrollViewer.VerticalOffset}, IsIntermediate={args.IsIntermediate}");

                    if (!args.IsIntermediate)
                    {
                        viewChanged.Set();
                    }
                };

                var itemsSource = new ObservableCollection<ObservableCollection<int>>();
                for (int i = 0; i < 100; i++)
                {
                    itemsSource.Add(new ObservableCollection<int>(Enumerable.Range(0, 5)));
                };

                rootRepeater.ItemsSource = itemsSource;
                Content = anchorProvider;
            });

            // scroll down several times to cause recycling of elements
            for (int i = 1; i < 10; i++)
            {
                IdleSynchronizer.Wait();
                RunOnUIThread.Execute(() =>
                {
                    Log.Comment($"Size=({rootRepeater.ActualWidth} x {rootRepeater.ActualHeight})");
                    Log.Comment($"ChangeView(VerticalOffset={i * 200})");
                    scrollViewer.ChangeView(null, i * 200, null, disableAnimation);
                });

                Log.Comment("Waiting for view change completion...");
                Verify.IsTrue(viewChanged.WaitOne(DefaultWaitTimeInMS));
                viewChanged.Reset();
                Log.Comment("View change completed");

                RunOnUIThread.Execute(() =>
                {
                    Verify.AreEqual(i * 200, scrollViewer.VerticalOffset);
                });
            }
            // }
        }

        // ScrollViewer scrolls vertically, but there is an inner 
        // repeater which flows horizontally which needs corrections to be handled.
        [TestMethod]
        public void VerifyCorrectionsInNonScrollableDirection()
        {
            ItemsRepeater rootRepeater = null;
            ScrollViewer scrollViewer = null;
            ItemsRepeaterScrollHost scrollhost = null;
            ManualResetEvent viewChanged = new ManualResetEvent(false);
            RunOnUIThread.Execute(() =>
            {
                scrollhost = (ItemsRepeaterScrollHost)XamlReader.Load(
                  @"<controls:ItemsRepeaterScrollHost Width='400' Height='600'
                     xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                     xmlns:controls='using:Microsoft.UI.Xaml.Controls'>
                    <ScrollViewer Width='400' Height='400' x:Name='scrollviewer'>
                        <controls:ItemsRepeater x:Name='repeater'>
                            <DataTemplate>
                                <StackPanel>
                                    <controls:ItemsRepeater ItemsSource='{Binding}'>
                                        <controls:ItemsRepeater.Layout>
                                            <controls:StackLayout Orientation='Horizontal' />
                                        </controls:ItemsRepeater.Layout>
                                    </controls:ItemsRepeater>
                                </StackPanel>
                            </DataTemplate>
                        </controls:ItemsRepeater>
                    </ScrollViewer>
                </controls:ItemsRepeaterScrollHost>");

                rootRepeater = (ItemsRepeater)scrollhost.FindName("repeater");
                scrollViewer = (ScrollViewer)scrollhost.FindName("scrollviewer");
                scrollViewer.ViewChanged += (sender, args) =>
                {
                    if (!args.IsIntermediate)
                    {
                        viewChanged.Set();
                    }
                };

                List<List<int>> items = new List<List<int>>();
                for (int i = 0; i < 100; i++)
                {
                    items.Add(Enumerable.Range(0, 4).ToList());
                }
                rootRepeater.ItemsSource = items;
                Content = scrollhost;
            });

            // scroll down several times and validate no crash
            for (int i = 1; i < 5; i++)
            {
                IdleSynchronizer.Wait();
                RunOnUIThread.Execute(() =>
                {
                    scrollViewer.ChangeView(null, i * 200, null);
                });

                Verify.IsTrue(viewChanged.WaitOne(DefaultWaitTimeInMS));
                viewChanged.Reset();
            }
        }


        [TestMethod]
        public void VerifyStoreScenarioCache()
        {
            ItemsRepeater rootRepeater = null;
            RunOnUIThread.Execute(() =>
            {
                var scrollhost = (ItemsRepeaterScrollHost)XamlReader.Load(
                  @" <controls:ItemsRepeaterScrollHost Width='400' Height='200'
                        xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                        xmlns:controls='using:Microsoft.UI.Xaml.Controls'>
                        <controls:ItemsRepeaterScrollHost.Resources>
                            <DataTemplate x:Key='ItemTemplate' >
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

                rootRepeater = (ItemsRepeater)scrollhost.FindName("rootRepeater");

                List<List<int>> items = new List<List<int>>();
                for (int i = 0; i < 100; i++)
                {
                    items.Add(Enumerable.Range(0, 4).ToList());
                }
                rootRepeater.ItemsSource = items;
                Content = scrollhost;
            });

            IdleSynchronizer.Wait();

            // Verify that first items outside the visible range but in the realized range
            // for the inner of the nested repeaters are realized.
            RunOnUIThread.Execute(() =>
            {
                // Group2 will be outside the visible range but within the realized range.
                var group2 = rootRepeater.TryGetElement(2) as StackPanel;
                Verify.IsNotNull(group2);

                var group2Repeater = ((ItemsRepeaterScrollHost)group2.Children[1]).ScrollViewer.Content as ItemsRepeater;
                Verify.IsNotNull(group2Repeater);

                Verify.IsNotNull(group2Repeater.TryGetElement(0));
            });
        }


        [TestMethod]
        public void VerifyUIElementsInItemsSource()
        {
            ItemsRepeater repeater = null;
            RunOnUIThread.Execute(() =>
            {
                var scrollhost = (ItemsRepeaterScrollHost)XamlReader.Load(
                  @"<controls:ItemsRepeaterScrollHost  
                     xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                     xmlns:local='using:MUXControlsTestApp.Samples'
                     xmlns:controls='using:Microsoft.UI.Xaml.Controls'>
                        <ScrollViewer>
                            <controls:ItemsRepeater x:Name='repeater'>
                                <controls:ItemsRepeater.ItemsSource>
                                    <local:UICollection>
                                        <Button>0</Button>
                                        <Button>1</Button>
                                        <Button>2</Button>
                                        <Button>3</Button>
                                        <Button>4</Button>
                                        <Button>5</Button>
                                        <Button>6</Button>
                                        <Button>7</Button>
                                        <Button>8</Button>
                                        <Button>9</Button>
                                    </local:UICollection>
                                </controls:ItemsRepeater.ItemsSource>
                            </controls:ItemsRepeater>
                        </ScrollViewer>
                    </controls:ItemsRepeaterScrollHost>");

                repeater = (ItemsRepeater)scrollhost.FindName("repeater");
                Content = scrollhost;
            });

            IdleSynchronizer.Wait();

            RunOnUIThread.Execute(() =>
            {
                for(int i=0; i<10;i++)
                {
                    var element = repeater.TryGetElement(i) as Button;
                    Verify.AreEqual(i.ToString(), element.Content);
                }
            });
        }
    }
}
