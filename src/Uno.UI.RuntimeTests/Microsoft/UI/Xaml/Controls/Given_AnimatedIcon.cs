#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.AnimatedVisuals;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public partial class Given_AnimatedIcon
{
	// Foreground is usually a ThemeResource on an app-lifetime brush: the brush must not root the icon (and its DataContext).
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Foreground_Set_While_Never_Loaded_Then_Not_Rooted_By_Brush(bool withSource)
	{
		var brush = new SolidColorBrush(Colors.Red);
		// A brush with a single parent keeps that parent alive on its own; a theme brush always has several.
		var otherOwner = new Border { Background = brush };
		var weakRef = CreateNeverLoadedIcon(brush, withSource);

		Assert.IsTrue(await TestHelper.TryWaitUntilCollected(weakRef), "A never-loaded AnimatedIcon is still rooted by its Foreground brush.");

		GC.KeepAlive(brush);
		GC.KeepAlive(otherOwner);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Foreground_Changes_After_Unload_Then_Not_Rooted_By_Brush(bool withSource)
	{
		var brush = new SolidColorBrush(Colors.Red);
		// A brush with a single parent keeps that parent alive on its own; a theme brush always has several.
		var otherOwner = new Border { Background = brush };
		var host = new Border { Width = 50, Height = 50 };
		await UITestHelper.Load(host);

		var weakRef = Attach(host, brush, withSource);
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsTrue(((AnimatedIcon)host.Child).IsLoaded);

		DetachAndChangeForeground(host, brush);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(await TestHelper.TryWaitUntilCollected(weakRef), "An AnimatedIcon whose Foreground changed after unloading is still rooted by its brush.");

		GC.KeepAlive(brush);
		GC.KeepAlive(otherOwner);
	}

	[TestMethod]
	public async Task When_Loaded_Then_Brush_Color_Change_Reaches_Source()
	{
		var brush = new SolidColorBrush(Colors.Red);
		var source = new ColorSpySource();
		var icon = new AnimatedIcon { Width = 20, Height = 20, Foreground = brush, Source = source };
		await UITestHelper.Load(icon);
		Assert.AreEqual(Colors.Red, source.Foreground);

		// The subscription to the brush must survive a GC while the icon is alive.
		GC.Collect();
		GC.WaitForPendingFinalizers();

		brush.Color = Colors.Blue;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(Colors.Blue, source.Foreground);
	}

	[TestMethod]
	public async Task When_Foreground_Brush_Swapped_While_Loaded_Then_New_Brush_Is_Tracked()
	{
		var oldBrush = new SolidColorBrush(Colors.Red);
		var newBrush = new SolidColorBrush(Colors.Green);
		var source = new ColorSpySource();
		var icon = new AnimatedIcon { Width = 20, Height = 20, Foreground = oldBrush, Source = source };
		await UITestHelper.Load(icon);

		icon.Foreground = newBrush;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(Colors.Green, source.Foreground);

		newBrush.Color = Colors.Blue;
		oldBrush.Color = Colors.Yellow;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(Colors.Blue, source.Foreground);
	}

	[TestMethod]
	public async Task When_Reloaded_Then_Current_Color_Applied_And_Brush_Tracked()
	{
		var brush = new SolidColorBrush(Colors.Red);
		var source = new ColorSpySource();
		var icon = new AnimatedIcon { Width = 20, Height = 20, Foreground = brush, Source = source };
		var host = new Border { Width = 50, Height = 50, Child = icon };
		await UITestHelper.Load(host);

		host.Child = null;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsFalse(icon.IsLoaded);
		brush.Color = Colors.Green;

		host.Child = icon;
		await TestServices.WindowHelper.WaitFor(() => icon.IsLoaded, message: "The icon was not reloaded.");
		Assert.AreEqual(Colors.Green, source.Foreground);

		brush.Color = Colors.Blue;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(Colors.Blue, source.Foreground);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateNeverLoadedIcon(SolidColorBrush brush, bool withSource)
	{
		var icon = new AnimatedIcon { DataContext = new object(), Foreground = brush };
		if (withSource)
		{
			icon.Source = new AnimatedAcceptVisualSource();
		}

		return new WeakReference(icon);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference Attach(Border host, SolidColorBrush brush, bool withSource)
	{
		var icon = new AnimatedIcon { Width = 20, Height = 20, Foreground = brush };
		if (withSource)
		{
			icon.Source = new AnimatedAcceptVisualSource();
		}
		host.Child = icon;
		return new WeakReference(icon);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void DetachAndChangeForeground(Border host, SolidColorBrush brush)
	{
		var icon = (AnimatedIcon)host.Child;
		host.Child = null;
		icon.Foreground = new SolidColorBrush(Colors.Blue);
		icon.Foreground = brush;
	}

	// Records the color AnimatedIcon pushes to its source; no visual, so the icon shows its (empty) fallback.
	private sealed partial class ColorSpySource : IAnimatedVisualSource2
	{
		public Color? Foreground { get; private set; }

		public IReadOnlyDictionary<string, double> Markers { get; } = new Dictionary<string, double>();

		public void SetColorProperty(string propertyName, Color value)
		{
			if (propertyName == "Foreground")
			{
				Foreground = value;
			}
		}

		public IAnimatedVisual? TryCreateAnimatedVisual(Compositor compositor, out object? diagnostics)
		{
			diagnostics = null;
			return null;
		}
	}
}
