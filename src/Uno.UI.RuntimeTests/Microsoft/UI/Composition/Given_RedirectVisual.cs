using System;
using System.Collections.Generic;
using System.Linq;
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
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)] // RedirectVisual requires composition
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
#endif
}
