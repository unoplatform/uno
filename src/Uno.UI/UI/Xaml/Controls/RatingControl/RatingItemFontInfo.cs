#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Controls
{
	[PortStatus(Complete = true)]

	public partial class RatingItemFontInfo : RatingItemInfo
	{
		[PortStatus("From Generated/3.x", Complete = true)]
		public string UnsetGlyph
		{
			get
			{
				return (string)this.GetValue(UnsetGlyphProperty);
			}
			set
			{
				this.SetValue(UnsetGlyphProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public string PointerOverPlaceholderGlyph
		{
			get
			{
				return (string)this.GetValue(PointerOverPlaceholderGlyphProperty);
			}
			set
			{
				this.SetValue(PointerOverPlaceholderGlyphProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public string PointerOverGlyph
		{
			get
			{
				return (string)this.GetValue(PointerOverGlyphProperty);
			}
			set
			{
				this.SetValue(PointerOverGlyphProperty, value);
			}
		}


		[PortStatus("From Generated/3.x", Complete = true)]
		public string PlaceholderGlyph
		{
			get
			{
				return (string)this.GetValue(PlaceholderGlyphProperty);
			}
			set
			{
				this.SetValue(PlaceholderGlyphProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public string Glyph
		{
			get
			{
				return (string)this.GetValue(GlyphProperty);
			}
			set
			{
				this.SetValue(GlyphProperty, value);
			}
		}

		[PortStatus("From Generated/3.x", Complete = true)]
		public string DisabledGlyph
		{
			get
			{
				return (string)this.GetValue(DisabledGlyphProperty);
			}
			set
			{
				this.SetValue(DisabledGlyphProperty, value);
			}
		}


		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty DisabledGlyphProperty { get; } =
		DependencyProperty.Register(
			"DisabledGlyph", typeof(string),
			typeof(RatingItemFontInfo),
			new FrameworkPropertyMetadata(default(string)));

		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty GlyphProperty { get; } =
		DependencyProperty.Register(
			"Glyph", typeof(string),
			typeof(RatingItemFontInfo),
			new FrameworkPropertyMetadata(default(string)));



		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty PlaceholderGlyphProperty { get; } =
		DependencyProperty.Register(
			"PlaceholderGlyph", typeof(string),
			typeof(RatingItemFontInfo),
			new FrameworkPropertyMetadata(default(string)));



		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty PointerOverGlyphProperty { get; } =
		DependencyProperty.Register(
			"PointerOverGlyph", typeof(string),
			typeof(RatingItemFontInfo),
			new FrameworkPropertyMetadata(default(string)));


		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty PointerOverPlaceholderGlyphProperty { get; } =
		DependencyProperty.Register(
			"PointerOverPlaceholderGlyph", typeof(string),
			typeof(RatingItemFontInfo),
			new FrameworkPropertyMetadata(default(string)));


		[PortStatus("From Generated/3.x", Complete = true)]
		public static DependencyProperty UnsetGlyphProperty { get; } =
		DependencyProperty.Register(
			"UnsetGlyph", typeof(string),
			typeof(RatingItemFontInfo),
			new FrameworkPropertyMetadata(default(string)));


		[PortStatus("From Generated/3.x", Complete = false)]
		public RatingItemFontInfo() : base()
		{
		}

		// Forced skipping of method Controls.RatingItemFontInfo.RatingItemFontInfo()
		// Forced skipping of method Controls.RatingItemFontInfo.DisabledGlyph.get
		// Forced skipping of method Controls.RatingItemFontInfo.DisabledGlyph.set
		// Forced skipping of method Controls.RatingItemFontInfo.Glyph.get
		// Forced skipping of method Controls.RatingItemFontInfo.Glyph.set
		// Forced skipping of method Controls.RatingItemFontInfo.PointerOverGlyph.get
		// Forced skipping of method Controls.RatingItemFontInfo.PointerOverGlyph.set
		// Forced skipping of method Controls.RatingItemFontInfo.PointerOverPlaceholderGlyph.get
		// Forced skipping of method Controls.RatingItemFontInfo.PointerOverPlaceholderGlyph.set
		// Forced skipping of method Controls.RatingItemFontInfo.PlaceholderGlyph.get
		// Forced skipping of method Controls.RatingItemFontInfo.PlaceholderGlyph.set
		// Forced skipping of method Controls.RatingItemFontInfo.UnsetGlyph.get
		// Forced skipping of method Controls.RatingItemFontInfo.UnsetGlyph.set
		// Forced skipping of method Controls.RatingItemFontInfo.DisabledGlyphProperty.get
		// Forced skipping of method Controls.RatingItemFontInfo.GlyphProperty.get
		// Forced skipping of method Controls.RatingItemFontInfo.PlaceholderGlyphProperty.get
		// Forced skipping of method Controls.RatingItemFontInfo.PointerOverGlyphProperty.get
		// Forced skipping of method Controls.RatingItemFontInfo.PointerOverPlaceholderGlyphProperty.get
		// Forced skipping of method Controls.RatingItemFontInfo.UnsetGlyphProperty.get
	}
}
