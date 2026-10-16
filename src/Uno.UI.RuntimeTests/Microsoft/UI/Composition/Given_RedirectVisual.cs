using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // RedirectVisual requires composition
public class Given_RedirectVisual
{
#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Source_Changes()
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var expected = new Image
		{
			Width = 200,
			Height = 200,
			Stretch = Stretch.UniformToFill
		};

		bool opened = false;
		expected.ImageOpened += (s, e) => opened = true;
		expected.Source = new BitmapImage(new Uri("https://uno-assets.platform.uno/logos/uno.png"));

		var sut = new ContentControl
		{
			Width = 200,
			Height = 200
		};

		var redirectVisual = compositor.CreateRedirectVisual(ElementCompositionPreview.GetElementVisual(expected));
		redirectVisual.Size = new(200, 200);

		ElementCompositionPreview.SetElementChildVisual(sut, redirectVisual);

		await UITestHelper.Load(new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(),
				new ColumnDefinition()
			},
			Children =
			{
				expected.Apply(e => Grid.SetColumn(e, 0)),
				sut.Apply(e => Grid.SetColumn(e, 1))
			}
		});

		await TestServices.WindowHelper.WaitFor(() => opened);

		var (expectedScreenshot, actualScreenshot) = (await UITestHelper.ScreenShot(expected), await UITestHelper.ScreenShot(sut));

		await ImageAssert.AreEqualAsync(actualScreenshot, expectedScreenshot);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Under_3D_Ancestor_Then_Matches_Source_In_Place()
	{
		// The square's Z offset goes through the ancestor's X rotation and moves it up. The redirect starts from the
		// live session's matrix, so it only lands in the same place if that matrix keeps its Z components.
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var tilt = Matrix4x4.CreateTranslation(-50, -50, 0) * Matrix4x4.CreateRotationX(MathF.PI / 3) * Matrix4x4.CreateTranslation(50, 50, 0);

		var square = compositor.CreateSpriteVisual();
		square.Size = new Vector2(40, 40);
		square.Offset = new Vector3(30, 30, 0);
		// Offset.Z isn't part of the local matrix, but a TransformMatrix translation is.
		square.TransformMatrix = Matrix4x4.CreateTranslation(0, 0, 40);
		square.Brush = compositor.CreateColorBrush(Microsoft.UI.Colors.Red);
		var inPlace = CreateTiltedHost(compositor, tilt, square);

		var redirect = compositor.CreateRedirectVisual(square);
		redirect.Size = new Vector2(100, 100);
		var redirected = CreateTiltedHost(compositor, tilt, redirect);

		using var _ = UITestHelper.ResetWindowContent();
		await UITestHelper.Load(new StackPanel { Children = { inPlace, redirected } });

		var expected = ImageAssert.GetColorBounds(await UITestHelper.ScreenShot(inPlace), Microsoft.UI.Colors.Red, tolerance: 10);
		var actual = ImageAssert.GetColorBounds(await UITestHelper.ScreenShot(redirected), Microsoft.UI.Colors.Red, tolerance: 10);

		Assert.IsTrue(expected.Width >= 10, $"The square didn't render in place (bounds: {expected}).");
		Assert.AreEqual(expected.Y, actual.Y, 2, $"Redirected square at {actual}, in place at {expected}.");
		Assert.AreEqual(expected.Height, actual.Height, 2, $"Redirected square at {actual}, in place at {expected}.");
	}

	private static Border CreateTiltedHost(Compositor compositor, Matrix4x4 tilt, Visual child)
	{
		var host = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Microsoft.UI.Colors.White) };
		var container = compositor.CreateContainerVisual();
		container.TransformMatrix = tilt;
		container.Children.InsertAtTop(child);
		ElementCompositionPreview.SetElementChildVisual(host, container);
		return host;
	}
#endif
}
