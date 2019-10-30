#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	public  partial class PersonPicture : Controls.Control
	{
		public  Media.ImageSource ProfilePicture
		{
			get => (Media.ImageSource)this.GetValue(ProfilePictureProperty);
			set => this.SetValue(ProfilePictureProperty, value);
		}

		public  bool PreferSmallImage
		{
			get => (bool)this.GetValue(PreferSmallImageProperty);
			set => this.SetValue(PreferSmallImageProperty, value);
		}

		public  bool IsGroup
		{
			get => (bool)this.GetValue(IsGroupProperty);
			set => this.SetValue(IsGroupProperty, value);
		}

		public  string Initials
		{
			get => (string)this.GetValue(InitialsProperty);
			set => this.SetValue(InitialsProperty, value);
		}

		public  string DisplayName
		{
			get => (string)this.GetValue(DisplayNameProperty);
			set => this.SetValue(DisplayNameProperty, value);
		}

		public  global::Windows.ApplicationModel.Contacts.Contact Contact
		{
			get => (global::Windows.ApplicationModel.Contacts.Contact)this.GetValue(ContactProperty);
			set => this.SetValue(ContactProperty, value);
		}

		public  string BadgeText
		{
			get => (string)this.GetValue(BadgeTextProperty);
			set => this.SetValue(BadgeTextProperty, value);
		}

		public  int BadgeNumber
		{
			get => (int)this.GetValue(BadgeNumberProperty);
			set => this.SetValue(BadgeNumberProperty, value);
		}

		public  Media.ImageSource BadgeImageSource
		{
			get => (Media.ImageSource)this.GetValue(BadgeImageSourceProperty);
			set => this.SetValue(BadgeImageSourceProperty, value);
		}

		public  string BadgeGlyph
		{
			get => (string)this.GetValue(BadgeGlyphProperty);
			set => this.SetValue(BadgeGlyphProperty, value);
		}

		public static DependencyProperty BadgeGlyphProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BadgeGlyph", typeof(string), 
			typeof(Controls.PersonPicture), 
			new FrameworkPropertyMetadata(""));

		public static DependencyProperty BadgeImageSourceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BadgeImageSource", typeof(Media.ImageSource), 
			typeof(Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(Media.ImageSource)));

		public static DependencyProperty BadgeNumberProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BadgeNumber", typeof(int), 
			typeof(Controls.PersonPicture), 
			new FrameworkPropertyMetadata(0));

		public static DependencyProperty BadgeTextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BadgeText", typeof(string), 
			typeof(Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(string)));

		public static DependencyProperty ContactProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Contact", typeof(global::Windows.ApplicationModel.Contacts.Contact), 
			typeof(Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(global::Windows.ApplicationModel.Contacts.Contact)));

		public static DependencyProperty DisplayNameProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DisplayName", typeof(string), 
			typeof(Controls.PersonPicture), 
			new FrameworkPropertyMetadata(""));

		public static DependencyProperty InitialsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Initials", typeof(string), 
			typeof(Controls.PersonPicture), 
			new FrameworkPropertyMetadata(""));

		public static DependencyProperty IsGroupProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsGroup", typeof(bool), 
			typeof(Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(bool)));

		public static DependencyProperty PreferSmallImageProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PreferSmallImage", typeof(bool), 
			typeof(Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(bool)));

		public static DependencyProperty ProfilePictureProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ProfilePicture", typeof(Media.ImageSource), 
			typeof(Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(Media.ImageSource)));
	}
}
