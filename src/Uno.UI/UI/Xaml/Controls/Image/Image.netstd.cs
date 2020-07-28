using System;
using System.Globalization;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Disposables;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Windows.UI;

namespace Windows.UI.Xaml.Controls
{
	partial class Image : FrameworkElement, ICustomClippingElement
	{
		/// <summary>
		/// When set, the resulting image is tentatively converted to Monochrome.
		/// </summary>
		internal Color? MonochromeColor { get; set; }

		#region Source DependencyProperty

		public ImageSource Source
		{
			get => (ImageSource)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		public static DependencyProperty SourceProperty { get; } =
			DependencyProperty.Register("Source", typeof(ImageSource), typeof(Image), new FrameworkPropertyMetadata(null, (s, e) => ((Image)s)?.OnSourceChanged(e)));

		partial void OnSourceChanged(DependencyPropertyChangedEventArgs e);
		#endregion

		public static DependencyProperty StretchProperty { get; } =
			DependencyProperty.Register(
				"Stretch",
				typeof(Stretch),
				typeof(Image),
				new FrameworkPropertyMetadata(
					Media.Stretch.Uniform,
					(s, e) => ((Image)s).OnStretchChanged((Stretch)e.NewValue, (Stretch)e.OldValue)));

		public Stretch Stretch
		{
			get => (Stretch)GetValue(StretchProperty);
			set => SetValue(StretchProperty, value);
		}

		private void OnStretchChanged(Stretch newValue, Stretch oldValue) => InvalidateArrange();

		internal override bool IsViewHit() => Source != null || base.IsViewHit();

		bool ICustomClippingElement.AllowClippingToLayoutSlot => true;

		bool ICustomClippingElement.ForceClippingToLayoutSlot => true;
	}
}
