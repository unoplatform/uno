// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbLayout.cpp, commit 9aedb00

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

#if !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
#endif

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests
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
                            xmlns:controls='using:Microsoft.UI.Xaml.Controls'>
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

		[TestMethod]
		public async Task VerifyLongBreadcrumbTrimsWithinRemainingWidth()
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				foreach (var flowDirection in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
				{
					var breadcrumb = CreateTextBreadcrumb(new[] { "Root", "Parent", new string('W', 100) });
					breadcrumb.FlowDirection = flowDirection;
					breadcrumb.Width = 2000;
					Content = breadcrumb;
					Content.UpdateLayout();

					var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
					var ellipsis = (BreadcrumbBarItem)repeater.TryGetElement(0);
					var lastItem = (BreadcrumbBarItem)repeater.TryGetElement(3);
					var presenter = (ContentPresenter)lastItem.FindVisualChildByName("PART_LastItemContentPresenter");
					var text = (TextBlock)presenter.FindVisualChildByName("CrumbText");
					double naturalWidth = lastItem.DesiredSize.Width;

					Verify.IsFalse(IsTextTrimmed(text));
					Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot(ellipsis).Width);

					breadcrumb.Width = 200;
					Content.UpdateLayout();

					var ellipsisSlot = LayoutInformation.GetLayoutSlot(ellipsis);
					var lastSlot = LayoutInformation.GetLayoutSlot(lastItem);
					Verify.IsGreaterThan(ellipsisSlot.Width, 0.0);
					Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot((FrameworkElement)repeater.TryGetElement(1)).Width);
					Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot((FrameworkElement)repeater.TryGetElement(2)).Width);
					Verify.IsTrue(lastSlot.Width <= breadcrumb.ActualWidth - ellipsisSlot.Width + 1);
					Verify.IsTrue(lastSlot.X + lastSlot.Width <= breadcrumb.ActualWidth + 1);
					Verify.IsTrue(IsTextTrimmed(text));
					VerifyTextIsInsideBreadcrumb(text, repeater);

					breadcrumb.Width = 2000;
					Content.UpdateLayout();

					Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot(ellipsis).Width);
					Verify.AreEqual(naturalWidth, lastItem.DesiredSize.Width);
					Verify.IsFalse(IsTextTrimmed(text));
					Verify.IsGreaterThan(LayoutInformation.GetLayoutSlot((FrameworkElement)repeater.TryGetElement(1)).Width, 0.0);

					breadcrumb.Width = 200;
					Content.UpdateLayout();
					Verify.AreEqual(lastSlot, LayoutInformation.GetLayoutSlot(lastItem));

					breadcrumb.Width = ellipsisSlot.Width / 2;
					Content.UpdateLayout();
					double pixelWidth = 1 / breadcrumb.XamlRoot.RasterizationScale;
					Verify.IsTrue(LayoutInformation.GetLayoutSlot(lastItem).Width <= pixelWidth + 0.001,
						"A tiny bar may leave at most one physical pixel after layout rounding.");

					breadcrumb.Width = 0;
					Content.UpdateLayout();
					Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot(lastItem).Width);
				}
			});
		}

		[TestMethod]
		public async Task VerifyOverflowPreservesFullyVisibleBreadcrumbs()
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var breadcrumb = CreateTextBreadcrumb(new[] { new string('W', 100), "Parent", "Current" });
				breadcrumb.Width = 2000;
				Content = breadcrumb;
				Content.UpdateLayout();

				var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				var ellipsis = (BreadcrumbBarItem)repeater.TryGetElement(0);
				var parentItem = (BreadcrumbBarItem)repeater.TryGetElement(2);
				var lastItem = (BreadcrumbBarItem)repeater.TryGetElement(3);
				double parentWidth = parentItem.DesiredSize.Width;
				double lastWidth = lastItem.DesiredSize.Width;

				breadcrumb.Width = ellipsis.DesiredSize.Width + parentWidth + lastWidth + 1;
				Content.UpdateLayout();

				Verify.IsGreaterThan(LayoutInformation.GetLayoutSlot(ellipsis).Width, 0.0);
				Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot((FrameworkElement)repeater.TryGetElement(1)).Width);
				Verify.AreEqual(parentWidth, LayoutInformation.GetLayoutSlot(parentItem).Width);
				Verify.AreEqual(lastWidth, LayoutInformation.GetLayoutSlot(lastItem).Width);
				var lastSlot = LayoutInformation.GetLayoutSlot(lastItem);
				Verify.IsTrue(lastSlot.X + lastSlot.Width <= breadcrumb.ActualWidth + 1);

				var parentPresenter = (ContentPresenter)parentItem.FindVisualChildByName("PART_ItemContentPresenter");
				var lastPresenter = (ContentPresenter)lastItem.FindVisualChildByName("PART_LastItemContentPresenter");
				var parentText = (TextBlock)parentPresenter.FindVisualChildByName("CrumbText");
				var lastText = (TextBlock)lastPresenter.FindVisualChildByName("CrumbText");
				Verify.IsFalse(IsTextTrimmed(parentText));
				Verify.IsFalse(IsTextTrimmed(lastText));
				VerifyTextIsInsideBreadcrumb(parentText, repeater);
				VerifyTextIsInsideBreadcrumb(lastText, repeater);
			});
		}

		[TestMethod]
		public async Task VerifySingleBreadcrumbTrimsWithoutOverflow()
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var breadcrumb = CreateTextBreadcrumb(new[] { new string('W', 100) });
				breadcrumb.Width = 200;
				var stackPanel = new StackPanel();
				stackPanel.Children.Add(breadcrumb);
				Content = stackPanel;
				Content.UpdateLayout();

				var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				var ellipsis = (BreadcrumbBarItem)repeater.TryGetElement(0);
				var lastItem = (BreadcrumbBarItem)repeater.TryGetElement(1);
				var presenter = (ContentPresenter)lastItem.FindVisualChildByName("PART_LastItemContentPresenter");
				var text = (TextBlock)presenter.FindVisualChildByName("CrumbText");
				Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot(ellipsis).Width);
				Verify.IsTrue(IsTextTrimmed(text));
				VerifyTextIsInsideBreadcrumb(text, repeater);

				breadcrumb.ClearValue(FrameworkElement.WidthProperty);
				stackPanel.Orientation = Orientation.Horizontal;
				Content.UpdateLayout();

				Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot(ellipsis).Width);
				Verify.IsFalse(IsTextTrimmed(text));
			});
		}

		[TestMethod]
		public async Task VerifyVisibleAncestorIsConstrained()
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var breadcrumb = new BreadcrumbBar
				{
					Width = 200,
					ItemsSource = new[] { new string('W', 100), string.Empty },
					ItemTemplate = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                        xmlns:controls='using:Microsoft.UI.Xaml.Controls'>
                            <controls:BreadcrumbBarItem Content='{Binding}'>
                                <controls:BreadcrumbBarItem.Template>
                                    <ControlTemplate TargetType='controls:BreadcrumbBarItem'>
                                        <ContentPresenter Content='{TemplateBinding Content}'>
                                            <ContentPresenter.ContentTemplate>
                                                <DataTemplate>
                                                    <TextBlock x:Name='CrumbText' Text='{Binding}' TextTrimming='CharacterEllipsis' IsTextScaleFactorEnabled='False'/>
                                                </DataTemplate>
                                            </ContentPresenter.ContentTemplate>
                                        </ContentPresenter>
                                    </ControlTemplate>
                                </controls:BreadcrumbBarItem.Template>
                            </controls:BreadcrumbBarItem>
                        </DataTemplate>")
				};
				Content = breadcrumb;
				Content.UpdateLayout();

				var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				var parentItem = (BreadcrumbBarItem)repeater.TryGetElement(1);
				var text = (TextBlock)parentItem.FindVisualChildByName("CrumbText");
				Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot((FrameworkElement)repeater.TryGetElement(0)).Width);
				Verify.IsTrue(LayoutInformation.GetLayoutSlot(parentItem).Width <= breadcrumb.ActualWidth + 1);
				Verify.IsTrue(IsTextTrimmed(text));
				VerifyTextIsInsideBreadcrumb(text, repeater);
			});
		}

		[TestMethod]
		public async Task VerifyConstrainedBreadcrumbUpdatesDesiredHeight()
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var breadcrumb = new BreadcrumbBar
				{
					Width = 2000,
					ItemsSource = new[] { "Root", string.Join(" ", Enumerable.Repeat("Folder", 30)) },
					ItemTemplate = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='{Binding}' TextWrapping='Wrap' FontSize='14' IsTextScaleFactorEnabled='False'/>
                        </DataTemplate>")
				};
				Content = breadcrumb;
				Content.UpdateLayout();

				var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				var lastItem = (BreadcrumbBarItem)repeater.TryGetElement(2);
				double naturalHeight = lastItem.DesiredSize.Height;

				breadcrumb.Width = 200;
				Content.UpdateLayout();

				Verify.IsGreaterThan(lastItem.DesiredSize.Height, naturalHeight);
				Verify.IsTrue(breadcrumb.DesiredSize.Height >= lastItem.DesiredSize.Height);
			});
		}

		[TestMethod]
		public async Task VerifyHiddenBreadcrumbDoesNotWrap()
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var breadcrumb = new BreadcrumbBar
				{
					Width = 2000,
					ItemsSource = new[] { string.Join(" ", Enumerable.Repeat("Ancestor", 100)), "Current" },
					ItemTemplate = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <TextBlock Text='{Binding}' TextWrapping='Wrap' FontSize='14' IsTextScaleFactorEnabled='False'/>
                        </DataTemplate>")
				};
				Content = breadcrumb;
				Content.UpdateLayout();
				double naturalHeight = breadcrumb.DesiredSize.Height;

				breadcrumb.Width = 200;
				Content.UpdateLayout();

				var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot((FrameworkElement)repeater.TryGetElement(1)).Width);
				Verify.AreEqual(naturalHeight, breadcrumb.DesiredSize.Height);
			});
		}

		[TestMethod]
		public async Task VerifyBreadcrumbMeasureCachingDuringResize()
		{
			BreadcrumbBar breadcrumb = null;
			ItemsRepeater repeater = null;
			MeasureCountingElement rootItem = null;
			MeasureCountingElement parentItem = null;
			MeasureCountingElement lastItem = null;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				breadcrumb = new BreadcrumbBar
				{
					Width = 10000,
					ItemsSource = new[] { "Root", "Parent", "Current" }
				};
				Content = breadcrumb;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				rootItem = new MeasureCountingElement(50);
				parentItem = new MeasureCountingElement(60);
				lastItem = new MeasureCountingElement(500);
				SetItemContent((BreadcrumbBarItem)repeater.TryGetElement(1), rootItem, isLastItem: false);
				SetItemContent((BreadcrumbBarItem)repeater.TryGetElement(2), parentItem, isLastItem: false);
				SetItemContent((BreadcrumbBarItem)repeater.TryGetElement(3), lastItem, isLastItem: true);
				Content.UpdateLayout();
				Verify.IsGreaterThan(lastItem.MeasureCount, 0, "The content must be part of the layout.");

				int rootMeasureCount = rootItem.MeasureCount;
				int parentMeasureCount = parentItem.MeasureCount;

				foreach (double width in new[] { 1200.0, 1100.0, 1000.0 })
				{
					lastItem.MeasureCount = 0;
					breadcrumb.Width = width;
					Content.UpdateLayout();

					Verify.AreEqual(rootMeasureCount, rootItem.MeasureCount);
					Verify.AreEqual(parentMeasureCount, parentItem.MeasureCount);
					Verify.AreEqual(1, lastItem.MeasureCount, "The last item should receive one final constraint, even when everything fits.");
				}

				int lastMeasureCount = lastItem.MeasureCount;

				breadcrumb.Width = 200;
				Content.UpdateLayout();

				Verify.AreEqual(rootMeasureCount, rootItem.MeasureCount, "An unchanged ancestor should reuse its natural measurement.");
				Verify.AreEqual(parentMeasureCount, parentItem.MeasureCount, "An unchanged ancestor should reuse its natural measurement.");
				Verify.AreEqual(lastMeasureCount + 1, lastItem.MeasureCount, "The first shrink should only add the constrained measurement.");

				foreach (double width in new[] { 180.0, 160.0, 140.0 })
				{
					lastItem.MeasureCount = 0;
					breadcrumb.Width = width;
					Content.UpdateLayout();

					Verify.AreEqual(rootMeasureCount, rootItem.MeasureCount);
					Verify.AreEqual(parentMeasureCount, parentItem.MeasureCount);
					Verify.AreEqual(1, lastItem.MeasureCount, "The last item should only be measured at its final constraint.");
				}

				breadcrumb.Width = 0;
				Content.UpdateLayout();
				rootItem.MeasureCount = 0;
				parentItem.MeasureCount = 0;
				lastItem.MeasureCount = 0;
				repeater.InvalidateMeasure();
				Content.UpdateLayout();

				Verify.AreEqual(0, rootItem.MeasureCount, "Repeated zero-width layout should reuse cached measurements.");
				Verify.AreEqual(0, parentItem.MeasureCount);
				Verify.AreEqual(0, lastItem.MeasureCount);
			});
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(3)]
		public async Task VerifyConstrainedBreadcrumbCanReduceDesiredHeight(int itemCount)
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				var breadcrumb = new BreadcrumbBar
				{
					Width = 200,
					ItemsSource = Enumerable.Range(0, itemCount).Select(index => "Item " + index).ToArray(),
					ItemTemplate = (DataTemplate)XamlReader.Load(
						@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                        xmlns:controls='using:Microsoft.UI.Xaml.Controls'>
                            <controls:BreadcrumbBarItem>
                                <controls:BreadcrumbBarItem.Template>
                                    <ControlTemplate TargetType='controls:BreadcrumbBarItem'>
                                        <Viewbox>
                                            <Border Width='500' Height='500' Background='CornflowerBlue'/>
                                        </Viewbox>
                                    </ControlTemplate>
                                </controls:BreadcrumbBarItem.Template>
                            </controls:BreadcrumbBarItem>
                        </DataTemplate>")
				};
				var followingContent = new Border { Height = 20 };
				var stackPanel = new StackPanel();
				stackPanel.Children.Add(breadcrumb);
				stackPanel.Children.Add(followingContent);
				Content = stackPanel;
				Content.UpdateLayout();

				var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				var lastItem = (BreadcrumbBarItem)repeater.TryGetElement(itemCount);

				if (itemCount == 1)
				{
					Verify.AreEqual(200.0, lastItem.DesiredSize.Height);
				}

				Verify.AreEqual(200.0, breadcrumb.DesiredSize.Height,
					"The bar should use the constrained height, not the Viewbox's natural height.");
				Verify.AreEqual(200.0, LayoutInformation.GetLayoutSlot(followingContent).Y,
					"Following content should not be displaced by the natural Viewbox height.");
			});
		}

		// The perf2026 template variant is not used by Uno, so only the default template is covered.
		[TestMethod]
		[DataRow("i")]
		[DataRow("C:")]
		public async Task VerifyNarrowInlineAncestorRemainsReadable(string parent)
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				foreach (var flowDirection in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
				{
					var breadcrumb = CreateTextBreadcrumb(new[] { parent, new string('W', 100) });
					breadcrumb.FlowDirection = flowDirection;
					breadcrumb.Width = 2000;
					Content = breadcrumb;
					Content.UpdateLayout();

					var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
					var ellipsis = (BreadcrumbBarItem)repeater.TryGetElement(0);
					var ancestor = (BreadcrumbBarItem)repeater.TryGetElement(1);
					var lastItem = (BreadcrumbBarItem)repeater.TryGetElement(2);
					double naturalWidth = ancestor.DesiredSize.Width;
					Verify.IsTrue(double.IsNaN(ancestor.Width), "The ancestor must not have an artificial width.");
					Verify.IsTrue(naturalWidth < ellipsis.DesiredSize.Width,
						"This readable ancestor should naturally cost less than the overflow button.");

					var presenter = (ContentPresenter)ancestor.FindVisualChildByName("PART_ItemContentPresenter");
					var label = (TextBlock)presenter.FindVisualChildByName("CrumbText");
					var chevron = (TextBlock)ancestor.FindVisualChildByName("PART_ChevronTextBlock");
					var lastPresenter = (ContentPresenter)lastItem.FindVisualChildByName("PART_LastItemContentPresenter");
					var lastLabel = (TextBlock)lastPresenter.FindVisualChildByName("CrumbText");
					double tolerance = 1 / breadcrumb.XamlRoot.RasterizationScale + 0.001;
					UIElement coordinateRoot = breadcrumb.XamlRoot.Content;
					Rect GetBounds(FrameworkElement element) => element.TransformToVisual(coordinateRoot)
						.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
					double GetGap(Rect first, Rect second) => Math.Max(
						second.X - (first.X + first.Width), first.X - (second.X + second.Width));
					double normalParentGap = GetGap(GetBounds(label), GetBounds(chevron));
					double normalTitleGap = GetGap(GetBounds(chevron), GetBounds(lastLabel));

					breadcrumb.Width = 200;
					Content.UpdateLayout();

					var ancestorBounds = GetBounds(ancestor);
					var labelBounds = GetBounds(label);
					var chevronBounds = GetBounds(chevron);
					var lastBounds = GetBounds(lastLabel);
					Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot(ellipsis).Width);
					Verify.AreEqual(parent, label.Text);
					Verify.IsFalse(IsTextTrimmed(label));
					Verify.IsGreaterThan(label.ActualWidth, 0.0);
					Verify.IsTrue(ancestor.ActualWidth + tolerance >= naturalWidth);
					Verify.IsTrue(labelBounds.X >= ancestorBounds.X - tolerance);
					Verify.IsTrue(labelBounds.X + labelBounds.Width <= ancestorBounds.X + ancestorBounds.Width + tolerance);
					Verify.IsTrue(chevronBounds.X >= ancestorBounds.X - tolerance);
					Verify.IsTrue(chevronBounds.X + chevronBounds.Width <= ancestorBounds.X + ancestorBounds.Width + tolerance);
					Verify.IsTrue(labelBounds.X + labelBounds.Width <= chevronBounds.X + tolerance ||
						chevronBounds.X + chevronBounds.Width <= labelBounds.X + tolerance);
					Verify.IsGreaterThan(chevron.Padding.Left, 0.0);
					Verify.IsGreaterThan(chevron.Padding.Right, 0.0);

					double glyphGap = Math.Max(
						lastBounds.X - (chevronBounds.X + chevronBounds.Width - chevron.Padding.Right),
						chevronBounds.X + chevron.Padding.Left - (lastBounds.X + lastBounds.Width));
					Verify.IsTrue(glyphGap >= Math.Min(chevron.Padding.Left, chevron.Padding.Right) - tolerance,
						"The separator must retain its normal padding before the final title.");
					Verify.IsTrue(Math.Abs(GetGap(labelBounds, chevronBounds) - normalParentGap) <= tolerance,
						"Parent-to-separator spacing should match the unconstrained layout.");
					Verify.IsTrue(Math.Abs(GetGap(chevronBounds, lastBounds) - normalTitleGap) <= tolerance,
						"Separator-to-title spacing should match the unconstrained layout.");
				}
			});
		}

		[TestMethod]
		public async Task VerifyWideAncestorPreservesCurrentItemSpace()
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				foreach (var flowDirection in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
				{
					var breadcrumb = CreateTextBreadcrumb(new[] { "Documents", new string('W', 100) });
					breadcrumb.FlowDirection = flowDirection;
					breadcrumb.Width = 200;
					Content = breadcrumb;
					Content.UpdateLayout();

					var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
					var ellipsis = (BreadcrumbBarItem)repeater.TryGetElement(0);
					var ancestor = (BreadcrumbBarItem)repeater.TryGetElement(1);
					var lastItem = (BreadcrumbBarItem)repeater.TryGetElement(2);
					var presenter = (ContentPresenter)lastItem.FindVisualChildByName("PART_LastItemContentPresenter");
					var text = (TextBlock)presenter.FindVisualChildByName("CrumbText");
					double inlineWidth = breadcrumb.Width - ancestor.DesiredSize.Width;
					double overflowWidth = breadcrumb.Width - ellipsis.DesiredSize.Width;
					double uncappedTextWidth = text.ActualWidth;

					Verify.IsTrue(double.IsNaN(ancestor.Width));
					Verify.AreEqual(double.PositiveInfinity, lastItem.MaxWidth);
					Verify.IsGreaterThan(ancestor.DesiredSize.Width, ellipsis.DesiredSize.Width);
					Verify.IsGreaterThan(lastItem.DesiredSize.Width, inlineWidth);
					Verify.IsTrue(lastItem.DesiredSize.Width <= overflowWidth,
						"The current item must be constrained to the space after overflow.");
					Verify.IsGreaterThan(LayoutInformation.GetLayoutSlot(ellipsis).Width, 0.0);
					Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot(ancestor).Width,
						"A wider parent must give way to overflow when the current item needs the space.");
					Verify.IsTrue(IsTextTrimmed(text));
					VerifyTextIsInsideBreadcrumb(text, repeater);

					foreach (bool fitsInline in new[] { true, false })
					{
						lastItem.MaxWidth = fitsInline ? inlineWidth - 1 : (inlineWidth + overflowWidth) / 2;
						Content.UpdateLayout();

						Verify.IsTrue(lastItem.DesiredSize.Width <= lastItem.MaxWidth);
						Verify.AreEqual(!fitsInline, LayoutInformation.GetLayoutSlot(ellipsis).Width > 0);
						Verify.AreEqual(fitsInline, LayoutInformation.GetLayoutSlot(ancestor).Width > 0);
						Verify.IsGreaterThan(uncappedTextWidth, text.ActualWidth,
							"An explicit MaxWidth, not the parent, should limit the title.");
						Verify.IsTrue(IsTextTrimmed(text));
						VerifyTextIsInsideBreadcrumb(text, repeater);
					}

					lastItem.MaxWidth = double.PositiveInfinity;
					Content.UpdateLayout();

					Verify.IsGreaterThan(LayoutInformation.GetLayoutSlot(ellipsis).Width, 0.0);
					Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot(ancestor).Width);
					Verify.AreEqual(uncappedTextWidth, text.ActualWidth,
						"Removing the explicit cap must restore all the space available after overflow.");
				}
			});
		}

		[TestMethod]
		[DataRow("MaxWidth", "Narrow")]
		[DataRow("MaxWidth", "Wide")]
		[DataRow("Content", "Narrow")]
		[DataRow("Content", "Wide")]
		[DataRow("TemplateText", "Narrow")]
		[DataRow("TemplateText", "Wide")]
		public async Task VerifyOverflowUpdatesAfterLastItemChanges(string change, string ancestors)
		{
			bool narrowAncestors = ancestors == "Narrow";
			string shortContent = new string('W', narrowAncestors ? 13 : 7);
			string longContent = new string('W', 100);
			BreadcrumbBar breadcrumb = null;
			BreadcrumbBar freshBreadcrumb = null;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				breadcrumb = CreateTextBreadcrumb(new[] { "i", shortContent });
				breadcrumb.Width = 2000;
				freshBreadcrumb = CreateTextBreadcrumb(new[] { "i", shortContent });
				freshBreadcrumb.Width = 2000;
				var stackPanel = new StackPanel();
				stackPanel.Children.Add(breadcrumb);
				stackPanel.Children.Add(freshBreadcrumb);
				Content = stackPanel;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				var ellipsis = (BreadcrumbBarItem)repeater.TryGetElement(0);
				var ancestor = (BreadcrumbBarItem)repeater.TryGetElement(1);
				var lastItem = (BreadcrumbBarItem)repeater.TryGetElement(2);
				// Artificial bounds isolate the size-cache regression; natural label visibility
				// and separator spacing are verified by VerifyNarrowInlineAncestorRemainsReadable.
				ancestor.Width = narrowAncestors ? 10 : 80;
				Content.UpdateLayout();

				double shortContentWidth = lastItem.DesiredSize.Width;
				Verify.AreEqual(narrowAncestors, ancestor.DesiredSize.Width < ellipsis.DesiredSize.Width);
				Verify.IsTrue(shortContentWidth + ancestor.DesiredSize.Width <= 200,
					"The short content and ancestor must fit without overflow.");

				if (narrowAncestors)
				{
					Verify.IsTrue(shortContentWidth + ellipsis.DesiredSize.Width > 200,
						"The short content must still be constrained if the stale overflow button remains.");
				}

				lastItem.Content = longContent;
				breadcrumb.Width = 200;
				Content.UpdateLayout();

				var presenter = (ContentPresenter)lastItem.FindVisualChildByName("PART_LastItemContentPresenter");
				var text = (TextBlock)presenter.FindVisualChildByName("CrumbText");
				Verify.AreEqual(!narrowAncestors, LayoutInformation.GetLayoutSlot(ellipsis).Width > 0);
				Verify.AreEqual(narrowAncestors, LayoutInformation.GetLayoutSlot(ancestor).Width > 0);
				Verify.IsTrue(IsTextTrimmed(text));

				switch (change)
				{
					case "MaxWidth":
						lastItem.MaxWidth = 200 - ancestor.DesiredSize.Width;
						break;
					case "Content":
						lastItem.Content = shortContent;
						break;
					case "TemplateText":
						text.Text = shortContent;
						break;
					default:
						throw new InvalidOperationException("Unexpected change: " + change);
				}

				Content.UpdateLayout();

				Verify.AreEqual(200.0, breadcrumb.ActualWidth);
				Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot(ellipsis).Width,
					"Overflow should disappear without resizing the bar or explicitly invalidating its measure.");
				Verify.IsGreaterThan(LayoutInformation.GetLayoutSlot(ancestor).Width, 0.0);

				if (change != "MaxWidth")
				{
					Verify.IsFalse(IsTextTrimmed(text), "The short content fits without trimming once overflow is removed.");
				}

				var freshRepeater = (ItemsRepeater)freshBreadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				var freshAncestor = (BreadcrumbBarItem)freshRepeater.TryGetElement(1);
				var freshLastItem = (BreadcrumbBarItem)freshRepeater.TryGetElement(2);
				freshAncestor.Width = ancestor.Width;
				freshBreadcrumb.Width = 200;

				if (change == "MaxWidth")
				{
					freshLastItem.Content = longContent;
					freshLastItem.MaxWidth = lastItem.MaxWidth;
				}

				Content.UpdateLayout();

				Verify.AreEqual(LayoutInformation.GetLayoutSlot((FrameworkElement)freshRepeater.TryGetElement(0)).Width,
					LayoutInformation.GetLayoutSlot(ellipsis).Width, "Cached and fresh overflow states should agree.");
				Verify.AreEqual(freshLastItem.DesiredSize, lastItem.DesiredSize,
					"Cached and fresh item measurements should agree after the change.");
				VerifyTextIsInsideBreadcrumb(text, repeater);

				if (change == "MaxWidth")
				{
					lastItem.MaxWidth = double.PositiveInfinity;
				}
				else if (change == "Content")
				{
					lastItem.Content = longContent;
				}
				else
				{
					text.Text = longContent;
				}

				Content.UpdateLayout();

				Verify.AreEqual(!narrowAncestors, LayoutInformation.GetLayoutSlot(ellipsis).Width > 0);
				Verify.AreEqual(narrowAncestors, LayoutInformation.GetLayoutSlot(ancestor).Width > 0);
				Verify.IsTrue(IsTextTrimmed(text));
			});
		}

		[TestMethod]
		public async Task VerifyZeroWidthOverflowUsesConstrainedAncestorWidth()
		{
			BreadcrumbBar breadcrumb = null;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				breadcrumb = CreateTextBreadcrumb(new[] { "x", "Ancestor", "Current" });
				breadcrumb.Width = 200;
				Content = breadcrumb;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				var ellipsis = (BreadcrumbBarItem)repeater.TryGetElement(0);
				var firstItem = (BreadcrumbBarItem)repeater.TryGetElement(1);
				var oversizedItem = (BreadcrumbBarItem)repeater.TryGetElement(2);
				var lastItem = (BreadcrumbBarItem)repeater.TryGetElement(3);
				ellipsis.Width = 0;
				firstItem.Width = 10;
				lastItem.Width = 0;
				SetItemContent(oversizedItem, new MeasureCountingElement(500, 20), isLastItem: false);
				Content.UpdateLayout();

				Verify.IsTrue(firstItem.DesiredSize.Width + oversizedItem.DesiredSize.Width <= 200);
				Verify.AreEqual(10.0, LayoutInformation.GetLayoutSlot(firstItem).Width,
					"The first item should fit after constraining the oversized ancestor.");
				Verify.AreEqual(10.0, LayoutInformation.GetLayoutSlot(oversizedItem).X);
				var slot = LayoutInformation.GetLayoutSlot(oversizedItem);
				Verify.IsTrue(slot.X + slot.Width <= breadcrumb.ActualWidth + 1);
			});
		}

		[TestMethod]
		public async Task VerifyOverflowUsesFinalMeasuredWidth()
		{
			BreadcrumbBar breadcrumb = null;
			BreadcrumbBar freshBreadcrumb = null;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				breadcrumb = CreateTextBreadcrumb(new[] { "i", "Current" });
				freshBreadcrumb = CreateTextBreadcrumb(new[] { "i", "Current" });
				breadcrumb.Width = 200;
				freshBreadcrumb.Width = 200;
				var stackPanel = new StackPanel();
				stackPanel.Children.Add(breadcrumb);
				stackPanel.Children.Add(freshBreadcrumb);
				Content = stackPanel;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				var ancestor = (BreadcrumbBarItem)repeater.TryGetElement(1);
				var lastItem = (BreadcrumbBarItem)repeater.TryGetElement(2);
				var content = new MeasureCountingElement(500, 8);
				ancestor.Width = 40;
				SetItemContent(lastItem, content, isLastItem: true);
				Content.UpdateLayout();

				double measuredWidth = content.DesiredSize.Width;
				Verify.IsGreaterThan(measuredWidth, 0.0, "The content must be part of the layout.");
				Verify.IsGreaterThan(ancestor.DesiredSize.Width,
					((BreadcrumbBarItem)repeater.TryGetElement(0)).DesiredSize.Width);
				Verify.IsTrue(ancestor.DesiredSize.Width + lastItem.DesiredSize.Width <= 200,
					"The final measured sizes must fit even though the natural content does not.");
				Verify.AreEqual(0.0, LayoutInformation.GetLayoutSlot((FrameworkElement)repeater.TryGetElement(0)).Width);
				Verify.IsGreaterThan(LayoutInformation.GetLayoutSlot(ancestor).Width, 0.0);

				content.NaturalWidth = measuredWidth;
				Content.UpdateLayout();
				Verify.AreEqual(measuredWidth, content.DesiredSize.Width,
					"This change must collide with the old constrained desired width.");

				var freshRepeater = (ItemsRepeater)freshBreadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				((BreadcrumbBarItem)freshRepeater.TryGetElement(1)).Width = 40;
				var freshLastItem = (BreadcrumbBarItem)freshRepeater.TryGetElement(2);
				SetItemContent(freshLastItem, new MeasureCountingElement(measuredWidth, 8), isLastItem: true);
				Content.UpdateLayout();

				Verify.AreEqual(LayoutInformation.GetLayoutSlot((FrameworkElement)freshRepeater.TryGetElement(0)).Width,
					LayoutInformation.GetLayoutSlot((FrameworkElement)repeater.TryGetElement(0)).Width);
				Verify.AreEqual(freshLastItem.DesiredSize, lastItem.DesiredSize,
					"An unchanged constrained size must produce the same result as a fresh layout.");
			});
		}

		[TestMethod]
		public async Task VerifyScalingContentMatchesFreshLayout()
		{
			BreadcrumbBar breadcrumb = null;
			BreadcrumbBar freshBreadcrumb = null;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				breadcrumb = CreateScalingBreadcrumb(500);
				freshBreadcrumb = CreateScalingBreadcrumb(100);
				var stackPanel = new StackPanel();
				stackPanel.Children.Add(breadcrumb);
				stackPanel.Children.Add(freshBreadcrumb);
				Content = stackPanel;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var repeater = (ItemsRepeater)breadcrumb.FindVisualChildByName("PART_ItemsRepeater");
				var lastItem = (BreadcrumbBarItem)repeater.TryGetElement(1);
				var viewbox = (Viewbox)lastItem.FindVisualChildByName("ScalingRoot");
				var child = (Border)viewbox.Child;
				child.Width = 100;
				child.Height = 100;
				Content.UpdateLayout();

				Verify.AreEqual(200.0, breadcrumb.DesiredSize.Height,
					"Scaling content should use its available width, not its intrinsic width.");
				Verify.AreEqual(200.0, freshBreadcrumb.DesiredSize.Height);
				Verify.AreEqual(freshBreadcrumb.DesiredSize, breadcrumb.DesiredSize,
					"Cached and fresh Viewbox content should have the same layout.");
			});
		}

		// The template binds Content to two presenters, and a UIElement can only have one parent,
		// so assign directly to the presenter that is shown.
		private static void SetItemContent(BreadcrumbBarItem item, UIElement content, bool isLastItem)
		{
			var presenter = (ContentPresenter)item.FindVisualChildByName(isLastItem ? "PART_LastItemContentPresenter" : "PART_ItemContentPresenter");
			presenter.ContentTemplate = null;
			presenter.Content = content;
		}

		private static BreadcrumbBar CreateScalingBreadcrumb(double size)
		{
			return new BreadcrumbBar
			{
				Width = 200,
				ItemsSource = new[] { size },
				ItemTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                    xmlns:controls='using:Microsoft.UI.Xaml.Controls'>
                        <controls:BreadcrumbBarItem>
                            <controls:BreadcrumbBarItem.Template>
                                <ControlTemplate TargetType='controls:BreadcrumbBarItem'>
                                    <Viewbox x:Name='ScalingRoot'>
                                        <Border Width='{Binding}' Height='{Binding}' Background='CornflowerBlue'/>
                                    </Viewbox>
                                </ControlTemplate>
                            </controls:BreadcrumbBarItem.Template>
                        </controls:BreadcrumbBarItem>
                    </DataTemplate>")
			};
		}

		private partial class MeasureCountingElement : FrameworkElement
		{
			private double _naturalWidth;
			private readonly double _constrainedWidthReduction;

			public MeasureCountingElement(double naturalWidth, double constrainedWidthReduction = 0)
			{
				_naturalWidth = naturalWidth;
				_constrainedWidthReduction = constrainedWidthReduction;
			}

			public int MeasureCount { get; set; }

			public double NaturalWidth
			{
				get => _naturalWidth;
				set
				{
					_naturalWidth = value;
					InvalidateMeasure();
				}
			}

			protected override Size MeasureOverride(Size availableSize)
			{
				++MeasureCount;
				double width = _naturalWidth <= availableSize.Width
					? _naturalWidth
					: Math.Max(0, availableSize.Width - _constrainedWidthReduction);
				return new Size(width, 20);
			}
		}

		private static BreadcrumbBar CreateTextBreadcrumb(IEnumerable<string> items)
		{
			return new BreadcrumbBar
			{
				ItemsSource = items,
				ItemTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <TextBlock x:Name='CrumbText' Text='{Binding}' TextTrimming='CharacterEllipsis' FontSize='14' IsTextScaleFactorEnabled='False'/>
                    </DataTemplate>")
			};
		}

		// Uno's TextBlock.IsTextTrimmed does not report CharacterEllipsis trimming, so compare against the natural width.
		private static bool IsTextTrimmed(TextBlock text)
		{
			var natural = new TextBlock { Text = text.Text, FontSize = text.FontSize, FontFamily = text.FontFamily, FontWeight = text.FontWeight };
			natural.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			return text.IsTextTrimmed || natural.DesiredSize.Width > text.ActualWidth + 0.5;
		}

		private static void VerifyTextIsInsideBreadcrumb(TextBlock text, ItemsRepeater repeater)
		{
			var bounds = text.TransformToVisual(repeater).TransformBounds(new Rect(0, 0, text.ActualWidth, text.ActualHeight));
			Verify.IsTrue(bounds.X >= -1);
			Verify.IsTrue(bounds.X + bounds.Width <= repeater.ActualWidth + 1);
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
                            xmlns:controls='using:Microsoft.UI.Xaml.Controls'
                            xmlns:local='using:Microsoft.UI.Xaml.Tests.MUXControls.ApiTests'>
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
                            xmlns:controls='using:Microsoft.UI.Xaml.Controls'
                            xmlns:local='using:Microsoft.UI.Xaml.Tests.MUXControls.ApiTests'>
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
                            xmlns:controls='using:Microsoft.UI.Xaml.Controls'
                            xmlns:local='using:Microsoft.UI.Xaml.Tests.MUXControls.ApiTests'>
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
