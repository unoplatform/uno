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
		// The popups take the root's XamlRoot, so the root must be in the live tree first.
		await TestServices.WindowHelper.WaitForLoaded(root);

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
			UITestHelper.CloseAllPopups();
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

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public async Task When_Transform_Shared_By_Brushes_Then_Discarded_Brushes_Collected()
	{
		// Transforms are shareable: one used by many brushes (e.g. a resource referenced from a template) outlives
		// them and must not keep the discarded ones alive.
		var transform = new TranslateTransform();

		var root = new StackPanel { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.White) };
		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			var brushRefs = CreateElementsWithTransformedBrush(root, transform, count: 5);
			await TestServices.WindowHelper.WaitForIdle();

			root.Children.Clear();
			await TestServices.WindowHelper.WaitForIdle();

			foreach (var brushRef in brushRefs)
			{
				Assert.IsTrue(await TestHelper.TryWaitUntilCollected(brushRef), "A discarded brush using the shared transform was not collected.");
			}

			// Red on the left half, blue on the right; moving the relative transform by half the width turns it all red.
			var survivor = new Border { Width = 40, Height = 10, HorizontalAlignment = HorizontalAlignment.Left, Background = CreateSplitBrush(transform) };
			root.Children.Add(survivor);
			await TestServices.WindowHelper.WaitForLoaded(survivor);
			await TestServices.WindowHelper.WaitForIdle();

			var before = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAt(before, 30, 5, Colors.Blue, tolerance: 5);

			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);

			transform.X = 0.5;
			await TestServices.WindowHelper.WaitForIdle();

			var after = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAt(after, 30, 5, Colors.Red, tolerance: 5);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}

		GC.KeepAlive(transform);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public async Task When_Transform_Shared_By_Geometries_Then_Discarded_Elements_Collected()
	{
		// A geometry listens to its transform and the element it clips to the geometry, so a transform shared across
		// geometries must not keep the discarded geometries, and through them their elements, alive.
		var transform = new TranslateTransform();

		var root = new StackPanel { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.White) };
		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			var elementRefs = CreateElementsWithTransformedClip(root, transform, count: 5);
			await TestServices.WindowHelper.WaitForIdle();

			root.Children.Clear();
			await TestServices.WindowHelper.WaitForIdle();

			foreach (var elementRef in elementRefs)
			{
				Assert.IsTrue(await TestHelper.TryWaitUntilCollected(elementRef), "A discarded element clipped by a geometry using the shared transform was not collected.");
			}

			// The clip shows the first 10 pixels of the element; moving it by 20 shows the 20-30 range instead.
			var survivor = CreateTransformedClipElement(transform);
			root.Children.Add(survivor);
			await TestServices.WindowHelper.WaitForLoaded(survivor);
			await TestServices.WindowHelper.WaitForIdle();

			var before = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAt(before, 5, 5, Colors.Red, tolerance: 5);
			ImageAssert.DoesNotHaveColorAt(before, 25, 5, Colors.Red, tolerance: 5);

			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);

			transform.X = 20;
			await TestServices.WindowHelper.WaitForIdle();

			var after = await UITestHelper.ScreenShot(root);
			ImageAssert.HasColorAt(after, 25, 5, Colors.Red, tolerance: 5);
			ImageAssert.DoesNotHaveColorAt(after, 5, 5, Colors.Red, tolerance: 5);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}

		GC.KeepAlive(transform);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
	public async Task When_Transform_Shared_By_TransformGroups_Then_Discarded_Groups_Collected()
	{
		// A TransformGroup listens to its children, so a child transform shared across groups must not keep the
		// discarded groups alive.
		var transform = new ScaleTransform { ScaleX = 2, ScaleY = 2 };

		var root = new StackPanel { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.White) };
		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			var groupRefs = CreateElementsWithTransformGroup(root, transform, count: 5);
			await TestServices.WindowHelper.WaitForIdle();

			root.Children.Clear();
			await TestServices.WindowHelper.WaitForIdle();

			foreach (var groupRef in groupRefs)
			{
				Assert.IsTrue(await TestHelper.TryWaitUntilCollected(groupRef), "A discarded TransformGroup containing the shared transform was not collected.");
			}

			var survivor = new Border
			{
				Width = 10,
				Height = 10,
				Background = new SolidColorBrush(Colors.Red),
				HorizontalAlignment = HorizontalAlignment.Left,
				RenderTransform = new TransformGroup { Children = { transform } },
			};
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
	public async Task When_TransformGroup_Children_Cleared_Then_Removed_Child_Collected()
	{
		// Clear() reports a Reset without the removed items; a long-lived group rebuilt in code must still let go of
		// the transforms it no longer contains, and keep following the ones added afterwards.
		var group = new TransformGroup();
		var childRef = AddChildTransform(group);

		group.Children.Clear();

		Assert.IsTrue(await TestHelper.TryWaitUntilCollected(childRef), "A transform cleared from a TransformGroup was not collected.");

		var newChild = new ScaleTransform { ScaleX = 3 };
		group.Children.Add(newChild);
		newChild.ScaleX = 4;

		Assert.AreEqual(4, group.Value.M11, "The group no longer follows a child added after Clear().");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference AddChildTransform(TransformGroup group)
	{
		var child = new ScaleTransform { ScaleX = 2 };
		group.Children.Add(child);
		return new WeakReference(child);
	}

	private static LinearGradientBrush CreateSplitBrush(Transform relativeTransform)
		=> new()
		{
			StartPoint = new(0, 0),
			EndPoint = new(1, 0),
			GradientStops =
			{
				new GradientStop { Color = Colors.Red, Offset = 0 },
				new GradientStop { Color = Colors.Red, Offset = 0.5 },
				new GradientStop { Color = Colors.Blue, Offset = 0.5 },
				new GradientStop { Color = Colors.Blue, Offset = 1 },
			},
			RelativeTransform = relativeTransform,
		};

	private static Border CreateTransformedClipElement(Transform transform)
		=> new()
		{
			Width = 40,
			Height = 10,
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = new SolidColorBrush(Colors.Red),
			Clip = new RectangleGeometry { Rect = new(0, 0, 10, 10), Transform = transform },
		};

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static List<WeakReference> CreateElementsWithTransformedBrush(Panel root, Transform transform, int count)
	{
		var refs = new List<WeakReference>(count);

		for (var i = 0; i < count; i++)
		{
			var brush = CreateSplitBrush(transform);
			root.Children.Add(new Border { Width = 40, Height = 10, Background = brush });
			refs.Add(new WeakReference(brush));
		}

		return refs;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static List<WeakReference> CreateElementsWithTransformedClip(Panel root, Transform transform, int count)
	{
		var refs = new List<WeakReference>(count);

		for (var i = 0; i < count; i++)
		{
			var element = CreateTransformedClipElement(transform);
			root.Children.Add(element);
			refs.Add(new WeakReference(element));
		}

		return refs;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static List<WeakReference> CreateElementsWithTransformGroup(Panel root, Transform transform, int count)
	{
		var refs = new List<WeakReference>(count);

		for (var i = 0; i < count; i++)
		{
			var group = new TransformGroup { Children = { transform } };
			root.Children.Add(new Border { Width = 10, Height = 10, RenderTransform = group });
			refs.Add(new WeakReference(group));
		}

		return refs;
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
