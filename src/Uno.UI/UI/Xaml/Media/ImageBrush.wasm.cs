using System;
using Windows.Foundation;
using Windows.UI.Xaml.Wasm;
using Uno.Disposables;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;
using Uno.Foundation.Logging;
using System.Collections.Concurrent;

namespace Windows.UI.Xaml.Media
{
	partial class ImageBrush
	{
		private static readonly IDictionary<string, Size> _naturalSizeCache = new Dictionary<string, Size>();
		private string _imageUri = string.Empty;

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
				AlignmentY.Top => "top",
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
				Stretch.Uniform => "contain", // patch for now
				Stretch.UniformToFill => "cover",
				_ => "auto"
			};
		}

		internal (UIElement defElement, IDisposable subscription) ToSvgElement(FrameworkElement target)
		{
			var pattern = new SvgElement("pattern");

			var preserveAspectRatio = SetPreserveAspectRatio();

			pattern.SetAttribute(
				("x", "0"),
				("y", "0"),
				("width", "100%"),
				("height", "100%")
			);

			var subscriptionDisposable = new SerialDisposable();
			var imageSourceChangedSubscription =
				this.RegisterDisposablePropertyChangedCallback(ImageSourceProperty, OnImageSourceChanged);

			var stretchChangedSubscription =
				this.RegisterDisposablePropertyChangedCallback(StretchProperty, OnStretchChanged);

			var alignmentXChangedSubscription =
				this.RegisterDisposablePropertyChangedCallback(AlignmentXProperty, OnAlignmentChanged);

			var alignmentYChangedSubscription =
				this.RegisterDisposablePropertyChangedCallback(AlignmentYProperty, OnAlignmentChanged);


			target.SizeChanged += OnTargetSizeChanged;
			var layoutUpdatedSubscription = Disposable.Create(() => target.SizeChanged -= OnTargetSizeChanged);

			void OnImageSourceChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
			{
				var newImageSource = (args.NewValue as ImageSource);
				subscriptionDisposable.Disposable = newImageSource?.Subscribe(OnSourceOpened);
			}

			void OnStretchChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
			{
				preserveAspectRatio = SetPreserveAspectRatio();
			}

			void OnAlignmentChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
			{
				preserveAspectRatio = SetPreserveAspectRatio();
			}

			void OnTargetSizeChanged(object sender, SizeChangedEventArgs e)
			{
				preserveAspectRatio = SetPreserveAspectRatio();
			}

			subscriptionDisposable.Disposable = ImageSource?.Subscribe(OnSourceOpened);

			void OnSourceOpened(ImageData data)
			{
				switch (data.Kind)
				{
					case ImageDataKind.Empty:
						pattern.ClearChildren();
						_imageUri = null;
						break;
					case ImageDataKind.DataUri:
					case ImageDataKind.Url:
						_imageUri = data.Value;

						var image = new SvgElement("image");
						image.SetAttribute(
							("width", "100%"),
							("height", "100%"),
							("preserveAspectRatio", preserveAspectRatio),
							("href", _imageUri)
						);

						// Clear any previous image, if any
						foreach (var previousChild in pattern.GetChildren())
						{
							pattern.RemoveChild(previousChild);
						}

						pattern.AddChild(image);

						if (Stretch == Stretch.None)
						{
							SetNaturalImageSize(pattern, target);
						}

						break;
				}
			}

			string SetPreserveAspectRatio()
			{
				var stretch = Stretch;

				var alignX = AlignmentX switch
				{
					AlignmentX.Left => "xMin",
					AlignmentX.Center => "xMid",
					AlignmentX.Right => "xMax",
					_ => string.Empty
				};
				var alignY = AlignmentY switch
				{
					AlignmentY.Top => "YMin",
					AlignmentY.Center => "YMid",
					AlignmentY.Bottom => "YMax",
					_ => string.Empty
				};

				var preserveAspectRatio = stretch switch
				{
					Stretch.None => $"{alignX}{alignY}",
					Stretch.Fill => "none",
					Stretch.Uniform => $"{alignX}{alignY} meet",
					Stretch.UniformToFill => $"{alignX}{alignY} slice",
					_ => string.Empty
				};

				if (stretch == Stretch.UniformToFill)
				{
					pattern.SetAttribute(
						("preserveAspectRatio", $"{preserveAspectRatio}")
					);
				}

				pattern.FindFirstChild()
					?.SetAttribute(("preserveAspectRatio", $"{preserveAspectRatio}"));

				if (Stretch == Stretch.None)
				{
					SetNaturalImageSize(pattern, target);
				}
				else
				{
					pattern.FindFirstChild()
						?.SetAttribute(
							("width", "100%"),
							("height", "100%")
						);

					pattern.RemoveAttribute("viewBox");
				}

				return preserveAspectRatio;
			}

			var subscriptions = new CompositeDisposable(
				imageSourceChangedSubscription,
				subscriptionDisposable,
				stretchChangedSubscription,
				alignmentXChangedSubscription,
				alignmentYChangedSubscription,
				layoutUpdatedSubscription
			);

			return (pattern, subscriptions);
		}

		//When Stretch is none, we preserve the original image size.
		//On WASM, in order to get the natural size of the image, it must first be loaded in an <img />.
		//We perform this action and return the <img />'s width and height from Uno.UI.WindowManager.current.getNaturalImageSize
		private async void SetNaturalImageSize(UIElement pattern, FrameworkElement target)
		{
			if (string.IsNullOrWhiteSpace(_imageUri))
			{
				return;
			}

			if (!_naturalSizeCache.TryGetValue(_imageUri, out var naturalSize))
			{
				var naturalSizeResponse = await WindowManagerInterop.GetNaturalImageSizeAsync(_imageUri);

				if (!TryParseNaturalSize(naturalSizeResponse, out naturalSize))
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"Error parsing response from Uno.UI.WindowManager.current.getNaturalImageSize. Attempted to parse value: {naturalSizeResponse}");
					}
					return;
				}

				if (!_naturalSizeCache.ContainsKey(_imageUri))
				{
					_naturalSizeCache.Add(_imageUri, naturalSize);
				}
			}

			var imgElement = pattern.FindFirstChild();

			imgElement?.SetAttribute(
				("width", naturalSize.Width.ToString(CultureInfo.InvariantCulture)),
				("height", naturalSize.Height.ToString(CultureInfo.InvariantCulture))
			);

			var width = (int)target.ActualWidth;
			var height = (int)target.ActualHeight;

			int viewBoxAlignX = AlignmentX switch
			{
				AlignmentX.Left => 0,
				AlignmentX.Center => (int)(naturalSize.Width - width) / 2,
				AlignmentX.Right => (int)(naturalSize.Width - width),
				_ => 0
			};

			int viewBoxAlignY = AlignmentY switch
			{
				AlignmentY.Top => 0,
				AlignmentY.Center => (int)(naturalSize.Height - height) / 2,
				AlignmentY.Bottom => (int)(naturalSize.Height - height),
				_ => 0
			};

			pattern.SetAttribute(("viewBox", $"{viewBoxAlignX} {viewBoxAlignY} {width} {height}"));
		}

		private bool TryParseNaturalSize(string sizeStr, out Size naturalSize)
		{
			naturalSize = default;

			if (string.IsNullOrWhiteSpace(sizeStr))
			{
				return false;
			}

			var parts = sizeStr.Split(';');
			if (parts.Length < 1)
			{
				return false;
			}

			if (!int.TryParse(parts[0], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var width)
				|| !int.TryParse(parts[1], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var height))
			{
				return false;
			}

			naturalSize = new Size(width, height);

			return true;
		}
	}
}
