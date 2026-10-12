#if HAS_UNO
#nullable enable

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.Graphics.Display;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_Graphics;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaFrameBuffer)]
public class Given_DisplayInformation_FrameBufferOrientation
{
	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public async Task When_Orientation_Changed_At_Runtime(bool quarterTurn)
	{
		var host = Application.Current.Host!;
		var setOrientation = GetHostSetOrientation(host);
		var displayInformation = DisplayInformation.GetForCurrentView();
		var original = displayInformation.CurrentOrientation;
		var originalSize = WindowHelper.XamlRoot.Size;
		var target = quarterTurn ? QuarterTurn(original) : HalfTurn(original);

		var orientationChangedCount = 0;
		void OnOrientationChanged(DisplayInformation sender, object args) => orientationChangedCount++;
		displayInformation.OrientationChanged += OnOrientationChanged;

		try
		{
			setOrientation(target);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(target, displayInformation.CurrentOrientation);
			Assert.AreEqual(1, orientationChangedCount);
			var expectedSize = quarterTurn ? new Size(originalSize.Height, originalSize.Width) : originalSize;
			AssertSize(expectedSize, WindowHelper.XamlRoot.Size);
		}
		finally
		{
			setOrientation(original);
			await WindowHelper.WaitForIdle();
			displayInformation.OrientationChanged -= OnOrientationChanged;
		}

		Assert.AreEqual(original, displayInformation.CurrentOrientation);
		AssertSize(originalSize, WindowHelper.XamlRoot.Size);
	}

	[TestMethod]
	[DataRow(DisplayOrientations.None)]
	[DataRow(DisplayOrientations.Landscape | DisplayOrientations.Portrait)]
	public void When_Builder_Setter_Gets_Not_A_Single_Orientation(DisplayOrientations value)
	{
		var (_, setOrientation) = CreateBuilderWithSetter(DisplayOrientations.Landscape);

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => setOrientation(value));
	}

	[TestMethod]
	public void When_Builder_Setter_Called_Before_Host_Exists()
	{
		var (builder, setOrientation) = CreateBuilderWithSetter(DisplayOrientations.Landscape);

		setOrientation(DisplayOrientations.PortraitFlipped);

		var startingOrientation = builder.GetType()
			.GetProperty("DisplayOrientation", BindingFlags.NonPublic | BindingFlags.Instance)!
			.GetValue(builder);
		Assert.AreEqual(DisplayOrientations.PortraitFlipped, startingOrientation);
	}

	// The test assembly doesn't reference the framebuffer runtime, so its API is reached by reflection.
	private static Action<DisplayOrientations> GetHostSetOrientation(object host)
	{
		var method = host.GetType().GetMethod("SetOrientation", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(DisplayOrientations)])
			?? throw new InvalidOperationException("The framebuffer host has no SetOrientation method.");
		return method.CreateDelegate<Action<DisplayOrientations>>(host);
	}

	private static (object builder, Action<DisplayOrientations> setOrientation) CreateBuilderWithSetter(DisplayOrientations orientation)
	{
		var builderType = Application.Current.Host!.GetType().Assembly.GetType("Uno.UI.Runtime.FramebufferHostBuilder", throwOnError: true)!;
		var builder = Activator.CreateInstance(builderType, nonPublic: true)!;
		var method = builderType.GetMethod("Orientation", [typeof(DisplayOrientations), typeof(Action<DisplayOrientations>).MakeByRefType()])
			?? throw new InvalidOperationException("The framebuffer host builder has no Orientation(orientation, out setter) method.");
		var args = new object?[] { orientation, null };
		method.Invoke(builder, args);
		return (builder, (Action<DisplayOrientations>)args[1]!);
	}

	private static DisplayOrientations QuarterTurn(DisplayOrientations orientation)
		=> orientation is DisplayOrientations.Portrait or DisplayOrientations.PortraitFlipped
			? DisplayOrientations.Landscape
			: DisplayOrientations.Portrait;

	private static DisplayOrientations HalfTurn(DisplayOrientations orientation)
		=> orientation switch
		{
			DisplayOrientations.Portrait => DisplayOrientations.PortraitFlipped,
			DisplayOrientations.PortraitFlipped => DisplayOrientations.Portrait,
			DisplayOrientations.LandscapeFlipped => DisplayOrientations.Landscape,
			_ => DisplayOrientations.LandscapeFlipped,
		};

	private static void AssertSize(Size expected, Size actual)
	{
		Assert.AreEqual(expected.Width, actual.Width, 1, $"Width: expected {expected}, got {actual}");
		Assert.AreEqual(expected.Height, actual.Height, 1, $"Height: expected {expected}, got {actual}");
	}
}
#endif
