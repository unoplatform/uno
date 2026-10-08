#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
public class Given_Transform
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public async Task When_RenderTransform_Shared_By_Style_Then_Discarded_Elements_Collected()
	{
		// A Style setter hands the same transform instance to every element the style applies to, so the
		// transform outlives the elements; it must not keep the discarded ones alive.
		var transform = new ScaleTransform { ScaleX = 2, ScaleY = 2 };
		var style = new Style(typeof(Border));
		style.Setters.Add(new Setter(UIElement.RenderTransformProperty, transform));

		// Fixed size: RenderTransform does not affect layout, so the screenshot must cover the scaled rendering.
		var root = new StackPanel { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.White) };
		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			var elementRefs = CreateStyledElements(root, style, count: 5);
			await TestServices.WindowHelper.WaitForIdle();

			root.Children.Clear();
			await TestServices.WindowHelper.WaitForIdle();

			foreach (var elementRef in elementRefs)
			{
				Assert.IsTrue(await TestHelper.TryWaitUntilCollected(elementRef), "A discarded element styled with the shared transform was not collected.");
			}

			// The shared transform must still drive the elements that do use it, even after a full collection: the
			// weak registration must stay alive exactly as long as the element's adapter does.
			var survivor = new Border { Width = 10, Height = 10, Background = new SolidColorBrush(Colors.Red), HorizontalAlignment = HorizontalAlignment.Left, Style = style };
			root.Children.Add(survivor);
			await TestServices.WindowHelper.WaitForLoaded(survivor);
			await TestServices.WindowHelper.WaitForIdle();

			var before = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAt(before, 15, 5, Colors.Red, tolerance: 5);
			ImageAssert.DoesNotHaveColorAt(before, 25, 5, Colors.Red, tolerance: 5);

			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);

			transform.ScaleX = 3;
			await TestServices.WindowHelper.WaitForIdle();

#if __SKIA__
			Assert.AreEqual(3, survivor.Visual.TransformMatrix.M11, "The shared transform no longer drives the surviving element's visual.");
#endif
			var after = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAt(after, 25, 5, Colors.Red, tolerance: 5);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}

		GC.KeepAlive(transform);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public async Task When_RenderTransform_Shared_By_Popup_Style_Then_Discarded_Popups_Collected()
	{
		// Popup forwards its RenderTransform changes to its PopupPanel through its own subscription, which must not
		// let a transform shared through a Style root discarded popups (and their content).
		var transform = new TranslateTransform { X = 20 };
		var style = new Style(typeof(Popup));
		style.Setters.Add(new Setter(UIElement.RenderTransformProperty, transform));

		var root = new Grid { Width = 200, Height = 200 };
		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			var popupRefs = await CreateStyledPopups(root, style, count: 5);
			await TestServices.WindowHelper.WaitForIdle();

			foreach (var popupRef in popupRefs)
			{
				Assert.IsTrue(await TestHelper.TryWaitUntilCollected(popupRef), "A discarded popup styled with the shared transform was not collected.");
			}

			// A surviving popup must still re-arrange its content when the shared transform changes, even after a
			// full collection: the weak registration must stay alive exactly as long as the popup does.
			var child = new Border { Width = 10, Height = 10 };
			var survivor = new Popup { Style = style, Child = child, XamlRoot = root.XamlRoot };
			root.Children.Add(survivor);
			survivor.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(child);
			await TestServices.WindowHelper.WaitForIdle();

			var before = child.TransformToVisual(null).TransformPoint(default).X;

			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);

			transform.X = 50;
			await TestServices.WindowHelper.WaitForIdle();

			var after = child.TransformToVisual(null).TransformPoint(default).X;
			Assert.AreEqual(before + 30, after, 0.5, "The shared transform no longer re-arranges the surviving popup's content.");
		}
		finally
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
			TestServices.WindowHelper.WindowContent = null;
		}

		GC.KeepAlive(transform);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public async Task When_Projection_Shared_By_Style_Then_Discarded_Elements_Collected()
	{
		// Same shape as RenderTransform: an element subscribes to its Projection, which a Style setter shares.
		var projection = new PlaneProjection { RotationY = 30 };
		var style = new Style(typeof(Border));
		style.Setters.Add(new Setter(UIElement.ProjectionProperty, projection));

		var root = new StackPanel { Width = 100, Height = 100 };
		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			var elementRefs = CreateStyledElements(root, style, count: 5);
			await TestServices.WindowHelper.WaitForIdle();

			root.Children.Clear();
			await TestServices.WindowHelper.WaitForIdle();

			foreach (var elementRef in elementRefs)
			{
				Assert.IsTrue(await TestHelper.TryWaitUntilCollected(elementRef), "A discarded element styled with the shared projection was not collected.");
			}

			var survivor = new Border { Width = 10, Height = 10, Style = style };
			root.Children.Add(survivor);
			await TestServices.WindowHelper.WaitForLoaded(survivor);
			await TestServices.WindowHelper.WaitForIdle();

#if __SKIA__
			var before = survivor.Visual.TransformMatrix;
#endif

			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);

			projection.RotationY = 60;
			await TestServices.WindowHelper.WaitForIdle();

#if __SKIA__
			Assert.AreNotEqual(before, survivor.Visual.TransformMatrix, "The shared projection no longer drives the surviving element's visual.");
#endif
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}

		GC.KeepAlive(projection);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<List<WeakReference>> CreateStyledPopups(Panel root, Style style, int count)
	{
		var refs = new List<WeakReference>(count);

		for (var i = 0; i < count; i++)
		{
			var popup = new Popup { Style = style, Child = new Border { Width = 10, Height = 10 }, XamlRoot = root.XamlRoot };
			root.Children.Add(popup);
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForIdle();
			popup.IsOpen = false;
			root.Children.Remove(popup);
			refs.Add(new WeakReference(popup));
		}

		return refs;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static List<WeakReference> CreateStyledElements(Panel root, Style style, int count)
	{
		var refs = new List<WeakReference>(count);

		for (var i = 0; i < count; i++)
		{
			var element = new Border { Width = 10, Height = 10, Style = style };
			root.Children.Add(element);
			refs.Add(new WeakReference(element));
		}

		return refs;
	}
}
