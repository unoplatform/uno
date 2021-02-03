using System;
using Windows.Foundation;
using Windows.UI.Xaml.Wasm;
using Uno.Disposables;

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
				AlignmentY.Top => "YMin",
				AlignmentY.Center => "YMid",
				AlignmentY.Bottom => "YMax",
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

			// Using this solution to set the viewBox/Size
			// https://stackoverflow.com/a/13915777/1176099

			pattern.SetAttribute(
				("x", "0"),
				("y", "0"),
				("width", "1"),
				("height", "1"),
				("viewBox", "0 0 100 100"),
				("preserveAspectRatio", alignX + alignY + " " + preserveAspectRatio));

			var subscriptionDisposable = new SerialDisposable();

			var imageSourceChangedSubscription =
				this.RegisterDisposablePropertyChangedCallback(ImageSourceProperty, OnImageSourceChanged);

			void OnImageSourceChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
			{
				var newImageSource = (args.NewValue as ImageSource);
				subscriptionDisposable.Disposable = newImageSource?.Subscribe(OnSourceOpened);
			}

			subscriptionDisposable.Disposable = ImageSource?.Subscribe(OnSourceOpened);

			void OnSourceOpened(ImageData data)
			{
				switch (data.Kind)
				{
					case ImageDataKind.Empty:
						pattern.SetHtmlContent("");
						break;
					case ImageDataKind.DataUri:
					case ImageDataKind.Url:
						pattern.SetHtmlContent($"<image width=\"100\" height=\"100\" xlink:href=\"{data.Value}\" />");
						break;
				}
			}

			var subscriptions = new CompositeDisposable(imageSourceChangedSubscription, subscriptionDisposable);

			return (pattern, subscriptions);
		}
	}
}
