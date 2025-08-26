using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SamplesApp.UITests;
using SkiaSharp;
using Uno.Disposables;
using Uno.Extensions.Specialized;
using Uno.UI.RuntimeTests.Helpers;
using Uno.WinUI.Graphics2DSK;
using CollectionExtensions = Uno.Extensions.CollectionExtensions;
using RectExtensions = Uno.Extensions.RectExtensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

#if __SKIA__
[TestClass]
[RunsOnUIThread]
public class Given_ContainerVisual
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_Children_Change()
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var containerVisual = compositor.CreateContainerVisual();
		Assert.IsFalse(containerVisual.IsChildrenRenderOrderDirty);

		var shape = compositor.CreateShapeVisual();
		containerVisual.Children.InsertAtTop(shape);
		Assert.IsTrue(containerVisual.IsChildrenRenderOrderDirty);
		var children = containerVisual.GetChildrenInRenderOrderTestingOnly();
		Assert.IsFalse(containerVisual.IsChildrenRenderOrderDirty);
		Assert.AreEqual(1, children.Count());

		containerVisual.Children.InsertAtTop(compositor.CreateShapeVisual());
		Assert.IsTrue(containerVisual.IsChildrenRenderOrderDirty);
		children = containerVisual.GetChildrenInRenderOrderTestingOnly();
		Assert.IsFalse(containerVisual.IsChildrenRenderOrderDirty);
		Assert.AreEqual(2, children.Count());

		containerVisual.Children.Remove(shape);
		Assert.IsTrue(containerVisual.IsChildrenRenderOrderDirty);
		children = containerVisual.GetChildrenInRenderOrderTestingOnly();
		Assert.IsFalse(containerVisual.IsChildrenRenderOrderDirty);
		Assert.AreEqual(1, children.Count());
	}

	[TestMethod]
	public async Task When_Child_Removed_TotalMatrix_Updated()
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var parent1 = compositor.CreateContainerVisual();
		var parent2 = compositor.CreateContainerVisual();
		var child = compositor.CreateSpriteVisual();

		parent2.Offset = new Vector3(100, 0, 0);

		parent1.Children.InsertAtTop(child);
		Assert.IsTrue(child.TotalMatrix.IsIdentity);

		parent2.Children.InsertAtTop(child);
		Assert.IsFalse(child.TotalMatrix.IsIdentity);
	}

	[TestMethod]
	public async Task When_SKPicture_Collapsing_Optimization()
	{
		var oldValues =
			(FeatureConfiguration.Rendering.EnableVisualSubtreeSkippingOptimization,
				FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationCleanFramesThreshold,
				FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationVisualCountThreshold);
		using var _ = Disposable.Create(() =>
		{
			(FeatureConfiguration.Rendering.EnableVisualSubtreeSkippingOptimization,
				FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationCleanFramesThreshold,
				FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationVisualCountThreshold) = oldValues;
		});
		(FeatureConfiguration.Rendering.EnableVisualSubtreeSkippingOptimization,
			FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationCleanFramesThreshold,
			FeatureConfiguration.Rendering.VisualSubtreeSkippingOptimizationVisualCountThreshold) = (true, 50, 100);
		var popup = new Popup()
		{
			Child = new StackPanel().Apply(sp => Enumerable.Range(0, 100).ForEach(i =>
				sp.Children.Add(new Rectangle
				{
					Fill = new SolidColorBrush(Colors.Red),
					Width = 100,
					Height = 100
				})))
		};

		var skce = new FrameCounterSKCanvasElement() { Width = 100, Height = 100 };
		popup.PlacementTarget = skce;
		popup.DesiredPlacement = PopupPlacementMode.Bottom;
		await UITestHelper.Load(new StackPanel { Children = { skce, popup } });

		for (int i = 0; i < 200; i++)
		{
			popup.IsOpen = !popup.IsOpen;
			await UITestHelper.WaitForIdle();
			var count = skce.FrameCounter;
			await UITestHelper.WaitFor(() => skce.FrameCounter > count);
		}

		popup.IsOpen = true;
		await UITestHelper.WaitForIdle();
		var p = (popup.Child as FrameworkElement).GetAbsoluteBoundsRect().GetLocation().Offset(50, 50);

		var screenShot1 = await UITestHelper.ScreenShot(((FrameworkElement)TestServices.WindowHelper.XamlRoot.VisualTree.RootElement)!);
		ImageAssert.HasColorAt(screenShot1, p, Colors.Red);

		popup.IsOpen = false;
		await UITestHelper.WaitForIdle();

		var screenShot2 = await UITestHelper.ScreenShot(((FrameworkElement)TestServices.WindowHelper.XamlRoot.VisualTree.RootElement)!);
		ImageAssert.DoesNotHaveColorAt(screenShot2, p, Colors.Red);
	}

	private class FrameCounterSKCanvasElement : SKCanvasElement
	{
		public int FrameCounter { get; private set; }
		protected override void RenderOverride(SKCanvas canvas, Size area)
		{
			FrameCounter++;
			Invalidate();
		}
	}
}
#endif
