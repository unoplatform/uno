using System;
using Windows.Foundation;
using Windows.UI.Xaml.Wasm;
using NotImplementedException = System.NotImplementedException;

namespace Windows.UI.Xaml.Media
{
	partial class ImageBrush
	{
		internal string ToCssPosition()
		{
			var x = AlignmentX switch
			{
				AlignmentX.Left => "left",
				AlignmentX.Center => "center",
				AlignmentX.Right => "right",
				_ => ""
			};

			var y = AlignmentY switch
			{
				AlignmentY.Top=> "top",
				AlignmentY.Center => "center",
				AlignmentY.Bottom => "bottom",
				_ => ""
			};

			return $"{x} {y}";
		}
		internal string ToCssBackgroundSize()
		{
			return Stretch switch
			{
				Stretch.Fill => "100% 100%",
				Stretch.None => "auto",
				Stretch.Uniform => "auto", // patch for now
				Stretch.UniformToFill => "auto", // patch for now
				_ => "auto"
			};
		}

		internal (UIElement defElement, IDisposable subscription) ToSvgElement()
		{
			var pattern = new SvgElement("pattern");

			var alignX = AlignmentX switch
			{
				AlignmentX.Left => "xMin",
				AlignmentX.Center => "xMid",
				AlignmentX.Right => "xMax",
				_ => ""
			};
			var alignY = AlignmentY switch
			{
				AlignmentY.Top => "yMin",
				AlignmentY.Center => "yMid",
				AlignmentY.Bottom => "yMax",
				_ => ""
			};

			var preserveAspectRatio = Stretch switch
				{
					Stretch.Fill => "none",
					Stretch.None => "",
					Stretch.Uniform => "meet",
					Stretch.UniformToFill => "slice",
					_ => "",
				};

			pattern.SetAttribute(
				("x", "0"),
				("y", "0"),
				("width", "1"),
				("height", "1"),
				("preserveAspectRatio", alignX + alignY + " " + preserveAspectRatio));

			var source = ImageSource;

			var subscription = source?.Subscribe(OnImageData);

			void OnImageData(ImageData data)
			{
				switch (data.Kind)
				{
					case ImageDataKind.Empty:
						pattern.SetHtmlContent("");
						break;
					case ImageDataKind.DataUri:
					case ImageDataKind.Url:
						pattern.SetHtmlContent($"<image xlink:href=\"{data.Value}\" />");
						break;
				}
			}

			return (pattern, subscription);
		}
	}
}
