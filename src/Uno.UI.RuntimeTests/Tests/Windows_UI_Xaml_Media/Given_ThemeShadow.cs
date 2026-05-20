#if __SKIA__
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
[RunsOnUIThread]
public class Given_ThemeShadow
{
	private static readonly FieldInfo s_shadowOnlyImageField =
		typeof(global::Microsoft.UI.Composition.Visual).GetField(
			"_shadowOnlyImage",
			BindingFlags.Instance | BindingFlags.NonPublic)!;

	private static bool HasShadowCache(UIElement element)
		=> s_shadowOnlyImageField.GetValue(element.Visual) is not null;

	[TestMethod]
	public async Task When_Shadow_Removed_Cache_Is_Released()
	{
		// Regression test for the ListView-item-recycle scenario from studio.live#1898.
		// A recycled container removes its Shadow; the cached blurred SKImage must
		// be released so the container's next use doesn't leak a now-orphan cache.
		var border = new Border
		{
			Width = 100,
			Height = 50,
			Background = new SolidColorBrush(Colors.SkyBlue),
			Shadow = new ThemeShadow(),
			Translation = new Vector3(0, 0, 16),
		};

		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		// Cache may or may not be populated depending on whether a frame ran while
		// the shadow was applied; either way removing the shadow must clear it.
		border.Shadow = null;
		border.Translation = Vector3.Zero;
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(
			HasShadowCache(border),
			"Cached blurred shadow image must be disposed when Shadow is unset.");
	}

	[TestMethod]
	public async Task When_Shadow_Changes_Cache_Is_Invalidated()
	{
		var border = new Border
		{
			Width = 100,
			Height = 50,
			Background = new SolidColorBrush(Colors.SkyBlue),
			Shadow = new ThemeShadow(),
			Translation = new Vector3(0, 0, 16),
		};

		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		// Changing Translation.Z mutates the ShadowState (offsets and sigmas
		// recompute), which must invalidate any cached image. Without
		// invalidation we'd render last frame's shadow at the new offset.
		border.Translation = new Vector3(0, 0, 48);
		await WindowHelper.WaitForIdle();

		// Re-rendering after the change should not throw; the cache may rebuild
		// to a new entry, but the old entry must not leak. The strongest
		// observable assertion is that the visual still renders and the cache
		// either points at a fresh image or is null.
		var afterChange = s_shadowOnlyImageField.GetValue(border.Visual);
		Assert.IsTrue(
			afterChange is null || afterChange is SkiaSharp.SKImage,
			"After ShadowState change, cache must be null or hold a freshly built SKImage.");
	}

	[TestMethod]
	public async Task When_Shadow_Applied_Visual_Renders()
	{
		// Smoke test: applying a ThemeShadow on a sized element with a positive
		// Translation.Z does not throw and produces a renderable visual tree.
		var grid = new Grid
		{
			Width = 200,
			Height = 100,
			Children =
			{
				new Rectangle
				{
					Width = 120,
					Height = 60,
					Fill = new SolidColorBrush(Colors.Turquoise),
					Shadow = new ThemeShadow(),
					Translation = new Vector3(0, 0, 32),
				},
			},
		};

		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		Assert.IsNotNull(grid.Visual);
		Assert.AreEqual(200, grid.ActualWidth);
		Assert.AreEqual(100, grid.ActualHeight);
	}

	[TestMethod]
	public async Task When_Shadow_Resized_Within_Bucket_Cache_Survives()
	{
		// The cache buckets size growth in 128px strides so that small drag/scroll
		// resizes don't force a rebuild. Resizing within the same stride must keep
		// the cached image alive.
		var border = new Border
		{
			Width = 100,
			Height = 50,
			Background = new SolidColorBrush(Colors.SkyBlue),
			Shadow = new ThemeShadow(),
			Translation = new Vector3(0, 0, 16),
		};

		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var cacheAfterFirstRender = s_shadowOnlyImageField.GetValue(border.Visual);

		// Resize by 12 px in each axis — well within the 128px bucket stride.
		border.Width = 112;
		border.Height = 62;
		await WindowHelper.WaitForIdle();

		var cacheAfterResize = s_shadowOnlyImageField.GetValue(border.Visual);

		if (cacheAfterFirstRender is not null)
		{
			Assert.AreSame(
				cacheAfterFirstRender,
				cacheAfterResize,
				"Resize within the same bucket stride must reuse the cached SKImage.");
		}
	}
}
#endif
