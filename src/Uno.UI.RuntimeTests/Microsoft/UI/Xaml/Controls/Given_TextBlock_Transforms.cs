using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TextBlock_Transforms
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24894")]
	[DataRow("None")]
	[DataRow("Translate")]
	[DataRow("Scale")]
	[DataRow("Rotate")]
	[DataRow("CompositeRotate")]
	[DataRow("CompositeScale")]
	[DataRow("RotatedBorder")]
	public async Task When_TextBlock_Has_RenderTransform_Then_Text_Is_Drawn(string kind)
	{
		var text = new TextBlock
		{
			Text = "Hello transformed text",
			FontSize = 20,
			Foreground = new SolidColorBrush(Colors.Black),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
		};

		UIElement content = text;
		Transform transform = kind switch
		{
			"Translate" => new TranslateTransform { X = 10 },
			"Scale" => new ScaleTransform { ScaleX = 1.5, ScaleY = 1.5 },
			"Rotate" => new RotateTransform { Angle = 10 },
			"CompositeRotate" => new CompositeTransform { Rotation = 10 },
			"CompositeScale" => new CompositeTransform { ScaleX = 1.5, ScaleY = 1.5 },
			_ => null,
		};

		if (kind == "RotatedBorder")
		{
			content = new Border
			{
				Child = text,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
				RenderTransform = new RotateTransform { Angle = 10 },
			};
		}
		else
		{
			text.RenderTransform = transform;
		}

		var host = new Grid
		{
			Width = 400,
			Height = 200,
			Background = new SolidColorBrush(Colors.White),
			Children = { content },
		};

		await UITestHelper.Load(host);
		var screenshot = await UITestHelper.ScreenShot(host);

		var dark = 0;
		for (var y = 0; y < screenshot.Height; y++)
		{
			for (var x = 0; x < screenshot.Width; x++)
			{
				var pixel = screenshot.GetPixel(x, y);
				if (pixel.R < 128 && pixel.G < 128 && pixel.B < 128)
				{
					dark++;
				}
			}
		}

		Assert.IsTrue(dark > 500, $"Only {dark} dark pixels were drawn for the text with a {kind} transform.");
	}
}
