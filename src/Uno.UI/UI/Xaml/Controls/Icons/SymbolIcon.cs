#nullable enable

using Windows.UI.Text;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using System;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents an icon that uses a glyph from the Segoe MDL2 Assets font as its content.
/// </summary>
public sealed partial class SymbolIcon : IconElement, IThemeChangeAware
{
	private double _fontSize = 20.0;

	private readonly TextBlock _textBlock;

	private static FontFamily? _symbolIconFontFamily;

	/// <summary>
	/// Initializes a new instance of the SymbolIcon class.
	/// </summary>
	public SymbolIcon() : this(Symbol.Emoji)
	{
	}

	/// <summary>
	/// Initializes a new instance of the SymbolIcon class using the specified symbol.
	/// </summary>
	/// <param name="symbol"></param>
	public SymbolIcon(Symbol symbol)
	{
		_textBlock = new TextBlock();
		AddIconChild(_textBlock);
		Symbol = symbol;

		SynchronizeProperties();
	}

	/// <summary>
	/// Gets or sets the Segoe MDL2 Assets glyph used as the icon content.
	/// </summary>
	public Symbol Symbol
	{
		get => (Symbol)GetValue(SymbolProperty);
		set => SetValue(SymbolProperty, value);
	}

	/// <summary>
	/// Identifies the Symbol dependency property.
	/// </summary>
	public static DependencyProperty SymbolProperty { get; } =
		DependencyProperty.Register(
			nameof(Symbol),
			typeof(Symbol),
			typeof(SymbolIcon),
			new FrameworkPropertyMetadata(Symbol.Emoji, propertyChangedCallback: (d, e) => ((SymbolIcon)d).SetSymbolText()));

	private void SynchronizeProperties()
	{
		_textBlock.Style = null;
		_textBlock.TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center;
		_textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
		_textBlock.VerticalAlignment = VerticalAlignment.Center;

		if (_fontSize > 0)
		{
			_textBlock.FontSize = _fontSize;
		}

		_textBlock.FontStyle = FontStyle.Normal;
		_textBlock.FontFamily = GetSymbolFontFamily();
		_textBlock.IsTextScaleFactorEnabled = false;
		_textBlock.SetValue(AutomationProperties.AccessibilityViewProperty, AccessibilityView.Raw);

		SetSymbolText();
		_textBlock.Foreground = Foreground;
	}

	internal void SetFontSize(double fontSize)
	{
		_fontSize = fontSize;
		if (fontSize == 0)
		{
			_textBlock.ClearValue(TextBlock.FontSizeProperty);
		}

		InvalidateMeasure();
	}

	private void SetSymbolText() => _textBlock.Text = ConvertSymbolValueToGlyph((int)Symbol).ToString();

	internal static char ConvertSymbolValueToGlyph(int symbolValue)
	{
		// Glyphs with prefixes ranging from E0- to E5- are marked as legacy to prevent unicode point collisions.
		// Some font sets require more characters and may program them in this range, causing icons to show as "jarbled characters".
		// Thus we are updating the enum to point to recommended unicode points in the upper E7 and above range.
		switch (symbolValue)
		{
			case 0xE10B: return (char)(0xE8FB); // Accept
			case 0xE168: return (char)(0xE910); // Account
			case 0xE109: return (char)(0xE710); // Add
			case 0xE1E2: return (char)(0xE8FA); // AddFriend
			case 0xE1A7: return (char)(0xE7EF); // Admin
			case 0xE1A1: return (char)(0xE8E3); // AlignCenter
			case 0xE1A2: return (char)(0xE8E4); // AlignLeft
			case 0xE1A0: return (char)(0xE8E2); // AlignRight
			case 0xE179: return (char)(0xE71D); // AllApps
			case 0xE16C: return (char)(0xE723); // Attach
			case 0xE12D: return (char)(0xE8A2); // AttachCamera
			case 0xE189: return (char)(0xE8D6); // Audio
			case 0xE112: return (char)(0xE72B); // Back
			case 0xE1D8: return (char)(0xE73F); // BackToWindow
			case 0xE1E0: return (char)(0xE8F8); // BlockContact
			case 0xE19B: return (char)(0xE8DD); // Bold
			case 0xE12F: return (char)(0xE8A4); // Bookmarks
			case 0xE155: return (char)(0xE7C5); // BrowsePhotos
			case 0xE133: return (char)(0xE8FD); // Bullets
			case 0xE1D0: return (char)(0xE8EF); // Calculator
			case 0xE163: return (char)(0xE787); // Calendar
			case 0xE161: return (char)(0xE8BF); // CalendarDay
			case 0xE1DB: return (char)(0xE8F5); // CalendarReply
			case 0xE162: return (char)(0xE8C0); // CalendarWeek
			case 0xE114: return (char)(0xE722); // Camera
			case 0xE10A: return (char)(0xE711); // Cancel
			case 0xE15A: return (char)(0xE8BA); // Caption
			case 0xE1C9: return (char)(0xE8EA); // CellPhone
			case 0xE164: return (char)(0xE8C1); // Character
			case 0xE106: return (char)(0xE894); // Clear
			case 0xE1C5: return (char)(0xE8E6); // ClearSelection
			case 0xE121: return (char)(0xE823); // Clock
			case 0xE190: return (char)(0xE7F0); // ClosedCaption
			case 0xE127: return (char)(0xE89F); // ClosePane
			case 0xE134: return (char)(0xE90A); // Comment
			case 0xE13D: return (char)(0xE77B); // Contact
			case 0xE187: return (char)(0xE8D4); // Contact2
			case 0xE136: return (char)(0xE779); // ContactInfo
			case 0xE181: return (char)(0xE8CF); // ContactPresence
			case 0xE16F: return (char)(0xE8C8); // Copy
			case 0xE123: return (char)(0xE7A8); // Crop
			case 0xE16B: return (char)(0xE8C6); // Cut
			case 0xE107: return (char)(0xE74D); // Delete
			case 0xE1D1: return (char)(0xE8F0); // Directions
			case 0xE194: return (char)(0xE8D8); // DisableUpdates
			case 0xE17A: return (char)(0xE8CD); // DisconnectDrive
			case 0xE19E: return (char)(0xE8E0); // Dislike
			case 0xE147: return (char)(0xE90E); // DockBottom
			case 0xE145: return (char)(0xE90C); // DockLeft
			case 0xE146: return (char)(0xE90D); // DockRight
			case 0xE130: return (char)(0xE8A5); // Document
			case 0xE118: return (char)(0xE896); // Download
			case 0xE104: return (char)(0xE70F); // Edit
			case 0xE11D: return (char)(0xE899); // Emoji
			case 0xE170: return (char)(0xE76E); // Emoji2
			case 0xE113: return (char)(0xE734); // Favorite
			case 0xE16E: return (char)(0xE71C); // Filter
			case 0xE11A: return (char)(0xE721); // Find
			case 0xE129: return (char)(0xE7C1); // Flag
			case 0xE188: return (char)(0xE8B7); // Folder
			case 0xE185: return (char)(0xE8D2); // Font
			case 0xE186: return (char)(0xE8D3); // FontColor
			case 0xE1C6: return (char)(0xE8E7); // FontDecrease
			case 0xE1C7: return (char)(0xE8E8); // FontIncrease
			case 0xE1C8: return (char)(0xE8E9); // FontSize
			case 0xE111: return (char)(0xE72A); // Forward
			case 0xE1E9: return (char)(0xE908); // FourBars
			case 0xE1D9: return (char)(0xE740); // FullScreen
			case 0xE700: return (char)(0xE700); // GlobalNavigationButton
			case 0xE12B: return (char)(0xE774); // Globe
			case 0xE143: return (char)(0xE8AD); // Go
			case 0xE1E4: return (char)(0xE8FC); // GoToStart
			case 0xE184: return (char)(0xE8D1); // GoToToday
			case 0xE137: return (char)(0xE778); // HangUp
			case 0xE11B: return (char)(0xE897); // Help
			case 0xE16A: return (char)(0xE8C5); // HideBcc
			case 0xE193: return (char)(0xE7E6); // Highlight
			case 0xE10F: return (char)(0xE80F); // Home
			case 0xE150: return (char)(0xE8B5); // Import
			case 0xE151: return (char)(0xE8B6); // ImportAll
			case 0xE171: return (char)(0xE8C9); // Important
			case 0xE199: return (char)(0xE8DB); // Italic
			case 0xE144: return (char)(0xE765); // Keyboard
			case 0xE11F: return (char)(0xE89B); // LeaveChat
			case 0xE1D3: return (char)(0xE8F1); // Library
			case 0xE19F: return (char)(0xE8E1); // Like
			case 0xE19D: return (char)(0xE8DF); // LikeDislike
			case 0xE167: return (char)(0xE71B); // Link
			case 0xE14C: return (char)(0xEA37); // List
			case 0xE119: return (char)(0xE715); // Mail
			case 0xE135: return (char)(0xE8A8); // MailFilled
			case 0xE120: return (char)(0xE89C); // MailForward
			case 0xE172: return (char)(0xE8CA); // MailReply
			case 0xE165: return (char)(0xE8C2); // MailReplyAll
			case 0xE178: return (char)(0xE912); // Manage
			case 0xE1C4: return (char)(0xE707); // Map
			case 0xE17B: return (char)(0xE8CE); // MapDrive
			case 0xE139: return (char)(0xE7B7); // MapPin
			case 0xE1D5: return (char)(0xE77C); // Memo
			case 0xE15F: return (char)(0xE8BD); // Message
			case 0xE1D6: return (char)(0xE720); // Microphone
			case 0xE10C: return (char)(0xE712); // More
			case 0xE19C: return (char)(0xE8DE); // MoveToFolder
			case 0xE142: return (char)(0xE90B); // MusicInfo
			case 0xE198: return (char)(0xE74F); // Mute
			case 0xE1DA: return (char)(0xE8F4); // NewFolder
			case 0xE17C: return (char)(0xE78B); // NewWindow
			case 0xE101: return (char)(0xE893); // Next
			case 0xE1E6: return (char)(0xE905); // OneBar
			case 0xE1A5: return (char)(0xE8E5); // OpenFile
			case 0xE197: return (char)(0xE8DA); // OpenLocal
			case 0xE126: return (char)(0xE8A0); // OpenPane
			case 0xE17D: return (char)(0xE7AC); // OpenWith
			case 0xE14F: return (char)(0xE8B4); // Orientation
			case 0xE1A6: return (char)(0xE7EE); // OtherUser
			case 0xE1CE: return (char)(0xE734); // OutlineStar
			case 0xE132: return (char)(0xE729); // Page
			case 0xE160: return (char)(0xE7C3); // Page2
			case 0xE16D: return (char)(0xE77F); // Paste
			case 0xE103: return (char)(0xE769); // Pause
			case 0xE125: return (char)(0xE716); // People
			case 0xE192: return (char)(0xE8D7); // Permissions
			case 0xE13A: return (char)(0xE717); // Phone
			case 0xE1D4: return (char)(0xE780); // PhoneBook
			case 0xE158: return (char)(0xE8B9); // Pictures
			case 0xE141: return (char)(0xE718); // Pin
			case 0xE18A: return (char)(0xE18A); // Placeholder
			case 0xE102: return (char)(0xE768); // Play
			case 0xE1D7: return (char)(0xE8F3); // PostUpdate
			case 0xE295: return (char)(0xE8FF); // Preview
			case 0xE12A: return (char)(0xE8A1); // PreviewLink
			case 0xE100: return (char)(0xE892); // Previous
			case 0xE749: return (char)(0xE749); // Print
			case 0xE182: return (char)(0xE8D0); // Priority
			case 0xE131: return (char)(0xE8A6); // ProtectedDocument
			case 0xE166: return (char)(0xE8C3); // Read
			case 0xE10D: return (char)(0xE7A6); // Redo
			case 0xE149: return (char)(0xE72C); // Refresh
			case 0xE148: return (char)(0xE8AF); // Remote
			case 0xE108: return (char)(0xE738); // Remove
			case 0xE13E: return (char)(0xE8AC); // Rename
			case 0xE15E: return (char)(0xE90F); // Repair
			case 0xE1CD: return (char)(0xE8EE); // RepeatAll
			case 0xE1CC: return (char)(0xE8ED); // RepeatOne
			case 0xE1DE: return (char)(0xE730); // ReportHacked
			case 0xE1CA: return (char)(0xE8EB); // ReShare
			case 0xE14A: return (char)(0xE7AD); // Rotate
			case 0xE124: return (char)(0xE89E); // RotateCamera
			case 0xE105: return (char)(0xE74E); // Save
			case 0xE159: return (char)(0xE78C); // SaveLocal
			case 0xE294: return (char)(0xE8FE); // Scan
			case 0xE14E: return (char)(0xE8B3); // SelectAll
			case 0xE122: return (char)(0xE724); // Send
			case 0xE18C: return (char)(0xE7B5); // SetLockScreen
			case 0xE18D: return (char)(0xE97B); // SetTile
			case 0xE115: return (char)(0xE713); // Setting
			case 0xE72D: return (char)(0xE72D); // Share
			case 0xE14D: return (char)(0xE719); // Shop
			case 0xE169: return (char)(0xE8C4); // ShowBcc
			case 0xE15C: return (char)(0xE8BC); // ShowResults
			case 0xE14B: return (char)(0xE8B1); // Shuffle
			case 0xE173: return (char)(0xE786); // SlideShow
			case 0xE1CF: return (char)(0xE735); // SolidStar
			case 0xE174: return (char)(0xE8CB); // Sort
			case 0xE15B: return (char)(0xE71A); // Stop
			case 0xE191: return (char)(0xE620); // StopSlideShow
			case 0xE1C3: return (char)(0xE913); // Street
			case 0xE13C: return (char)(0xE8AB); // Switch
			case 0xE1E1: return (char)(0xE8F9); // SwitchApps
			case 0xE117: return (char)(0xE895); // Sync
			case 0xE1DF: return (char)(0xE8F7); // SyncFolder
			case 0xE1CB: return (char)(0xE8EC); // Tag
			case 0xE1D2:
				return (char)(0xE1D2
			/*UNO TODO: Should be 0xF5F0, but it is missing in WINUI: https://github.com/microsoft/microsoft-ui-xaml/issues/10373*/); // Target
			case 0xE1E8: return (char)(0xE907); // ThreeBars
			case 0xE1E3: return (char)(0xE7C9); // TouchPointer
			case 0xE12C: return (char)(0xE78A); // Trim
			case 0xE1E7: return (char)(0xE906); // TwoBars
			case 0xE11E: return (char)(0xE89A); // TwoPage
			case 0xE19A: return (char)(0xE8DC); // Underline
			case 0xE10E: return (char)(0xE7A7); // Undo
			case 0xE195: return (char)(0xE8D9); // UnFavorite
			case 0xE196: return (char)(0xE77A); // UnPin
			case 0xE1DD: return (char)(0xE8F6); // UnSyncFolder
			case 0xE110: return (char)(0xE74A); // Up
			case 0xE11C: return (char)(0xE898); // Upload
			case 0xE116: return (char)(0xE714); // Video
			case 0xE13B: return (char)(0xE8AA); // VideoChat
			case 0xE18B: return (char)(0xE890); // View
			case 0xE138: return (char)(0xE8A9); // ViewAll
			case 0xE15D: return (char)(0xE767); // Volume
			case 0xE156: return (char)(0xE8B8); // WebCam
			case 0xE128: return (char)(0xE909); // World
			case 0xE990: return (char)(0xE990); // XboxOneConsole
			case 0xE1E5: return (char)(0xE904); // ZeroBars
			case 0xE1A3: return (char)(0xE71E); // Zoom
			case 0xE12E: return (char)(0xE8A3); // ZoomIn
			case 0xE1A4: return (char)(0xE71F); // ZoomOut
		}

		return (char)(symbolValue);
	}

	private static FontFamily GetSymbolFontFamily() =>
		 _symbolIconFontFamily ??= new FontFamily(Uno.UI.FeatureConfiguration.Font.SymbolsFont);

	private protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e)
	{
		// This may occur while executing the base constructor
		// so _textBlock may still be null.
		if (_textBlock is not null)
		{
			_textBlock.Foreground = (Brush)e.NewValue;
		}
	}

	// The way this works in WinUI is by the MarkInheritedPropertyDirty call in CFrameworkElement::NotifyThemeChangedForInheritedProperties
	// There is a special handling for Foreground specifically there.
	void IThemeChangeAware.OnThemeChanged()
	{
		if (_textBlock is not null)
		{
			_textBlock.Foreground = Foreground;
		}
	}
}
