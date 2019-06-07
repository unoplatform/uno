#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using Windows.UI.Xaml.Media;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Controls
{
	[PortStatus(Complete = true)]
	public partial class RatingItemImageInfo : RatingItemInfo
	{
		[PortStatus("From Generated/3.x", Complete = true)]
		public ImageSource UnsetImage
		{
			get
			{
				return (ImageSource)this.GetValue(UnsetImageProperty);
			}
			set
			{
				this.SetValue(UnsetImageProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public ImageSource PointerOverPlaceholderImage
		{
			get
			{
				return (ImageSource)this.GetValue(PointerOverPlaceholderImageProperty);
			}
			set
			{
				this.SetValue(PointerOverPlaceholderImageProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public ImageSource PointerOverImage
		{
			get
			{
				return (ImageSource)this.GetValue(PointerOverImageProperty);
			}
			set
			{
				this.SetValue(PointerOverImageProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public ImageSource PlaceholderImage
		{
			get
			{
				return (ImageSource)this.GetValue(PlaceholderImageProperty);
			}
			set
			{
				this.SetValue(PlaceholderImageProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public ImageSource Image
		{
			get
			{
				return (ImageSource)this.GetValue(ImageProperty);
			}
			set
			{
				this.SetValue(ImageProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public ImageSource DisabledImage
		{
			get
			{
				return (ImageSource)this.GetValue(DisabledImageProperty);
			}
			set
			{
				this.SetValue(DisabledImageProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty DisabledImageProperty { get; } =
		DependencyProperty.Register(
			"DisabledImage", typeof(ImageSource),
			typeof(RatingItemImageInfo),
			new FrameworkPropertyMetadata(default(ImageSource)));

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty ImageProperty { get; } =
		DependencyProperty.Register(
			"Image", typeof(ImageSource),
			typeof(RatingItemImageInfo),
			new FrameworkPropertyMetadata(default(ImageSource)));

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty PlaceholderImageProperty { get; } =
		DependencyProperty.Register(
			"PlaceholderImage", typeof(ImageSource),
			typeof(RatingItemImageInfo),
			new FrameworkPropertyMetadata(default(ImageSource)));

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty PointerOverImageProperty { get; } =
		DependencyProperty.Register(
			"PointerOverImage", typeof(ImageSource),
			typeof(RatingItemImageInfo),
			new FrameworkPropertyMetadata(default(ImageSource)));

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty PointerOverPlaceholderImageProperty { get; } =
		DependencyProperty.Register(
			"PointerOverPlaceholderImage", typeof(ImageSource),
			typeof(RatingItemImageInfo),
			new FrameworkPropertyMetadata(default(ImageSource)));

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty UnsetImageProperty { get; } =
		DependencyProperty.Register(
			"UnsetImage", typeof(ImageSource),
			typeof(RatingItemImageInfo),
			new FrameworkPropertyMetadata(default(ImageSource)));

		[PortStatus("From Generated/3.x", Complete = true)]
		public RatingItemImageInfo() : base()
		{
		}
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.RatingItemImageInfo()
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.DisabledImage.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.DisabledImage.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.Image.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.Image.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.PlaceholderImage.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.PlaceholderImage.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.PointerOverImage.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.PointerOverImage.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.PointerOverPlaceholderImage.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.PointerOverPlaceholderImage.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.UnsetImage.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.UnsetImage.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.DisabledImageProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.ImageProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.PlaceholderImageProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.PointerOverImageProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.PointerOverPlaceholderImageProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RatingItemImageInfo.UnsetImageProperty.get
	}
}
