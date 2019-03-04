#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PersonPicture : global::Windows.UI.Xaml.Controls.Control
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.ImageSource ProfilePicture
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.ImageSource)this.GetValue(ProfilePictureProperty);
			}
			set
			{
				this.SetValue(ProfilePictureProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool PreferSmallImage
		{
			get
			{
				return (bool)this.GetValue(PreferSmallImageProperty);
			}
			set
			{
				this.SetValue(PreferSmallImageProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsGroup
		{
			get
			{
				return (bool)this.GetValue(IsGroupProperty);
			}
			set
			{
				this.SetValue(IsGroupProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Initials
		{
			get
			{
				return (string)this.GetValue(InitialsProperty);
			}
			set
			{
				this.SetValue(InitialsProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DisplayName
		{
			get
			{
				return (string)this.GetValue(DisplayNameProperty);
			}
			set
			{
				this.SetValue(DisplayNameProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.Contacts.Contact Contact
		{
			get
			{
				return (global::Windows.ApplicationModel.Contacts.Contact)this.GetValue(ContactProperty);
			}
			set
			{
				this.SetValue(ContactProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string BadgeText
		{
			get
			{
				return (string)this.GetValue(BadgeTextProperty);
			}
			set
			{
				this.SetValue(BadgeTextProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  int BadgeNumber
		{
			get
			{
				return (int)this.GetValue(BadgeNumberProperty);
			}
			set
			{
				this.SetValue(BadgeNumberProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.ImageSource BadgeImageSource
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.ImageSource)this.GetValue(BadgeImageSourceProperty);
			}
			set
			{
				this.SetValue(BadgeImageSourceProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string BadgeGlyph
		{
			get
			{
				return (string)this.GetValue(BadgeGlyphProperty);
			}
			set
			{
				this.SetValue(BadgeGlyphProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BadgeGlyphProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BadgeGlyph", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BadgeImageSourceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BadgeImageSource", typeof(global::Windows.UI.Xaml.Media.ImageSource), 
			typeof(global::Windows.UI.Xaml.Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.ImageSource)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BadgeNumberProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BadgeNumber", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BadgeTextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BadgeText", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ContactProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Contact", typeof(global::Windows.ApplicationModel.Contacts.Contact), 
			typeof(global::Windows.UI.Xaml.Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(global::Windows.ApplicationModel.Contacts.Contact)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DisplayNameProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DisplayName", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty InitialsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Initials", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(string)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsGroupProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsGroup", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PreferSmallImageProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PreferSmallImage", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ProfilePictureProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ProfilePicture", typeof(global::Windows.UI.Xaml.Media.ImageSource), 
			typeof(global::Windows.UI.Xaml.Controls.PersonPicture), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.ImageSource)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public PersonPicture() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PersonPicture", "PersonPicture.PersonPicture()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.PersonPicture()
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.BadgeNumber.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.BadgeNumber.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.BadgeGlyph.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.BadgeGlyph.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.BadgeImageSource.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.BadgeImageSource.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.BadgeText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.BadgeText.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.IsGroup.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.IsGroup.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.Contact.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.Contact.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.DisplayName.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.DisplayName.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.Initials.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.Initials.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.PreferSmallImage.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.PreferSmallImage.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.ProfilePicture.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.ProfilePicture.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.BadgeNumberProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.BadgeGlyphProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.BadgeImageSourceProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.BadgeTextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.IsGroupProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.ContactProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.DisplayNameProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.InitialsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.PreferSmallImageProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PersonPicture.ProfilePictureProperty.get
	}
}
