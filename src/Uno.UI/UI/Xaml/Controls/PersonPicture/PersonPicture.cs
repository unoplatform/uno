// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PersonPicture.cpp, tag winui3/release/1.4.2

using System;
using System.Globalization;
using Uno.UI.Helpers.WinUI;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls;

public partial class PersonPicture : Control
{
	/// <summary>
	/// XAML Element for the first TextBlock matching x:Name of InitialsTextBlock.
	/// </summary>
	TextBlock m_initialsTextBlock;

	/// <summary>
	/// XAML Element for the first TextBlock matching x:Name of BadgeNumberTextBlock.
	/// </summary>
	TextBlock m_badgeNumberTextBlock;

	/// <summary>
	/// XAML Element for the first TextBlock matching x:Name of BadgeGlyphIcon.
	/// </summary>
	FontIcon m_badgeGlyphIcon;

	/// <summary>
	/// XAML Element for the first ImageBrush matching x:Name of BadgeImageBrush.
	/// </summary>
	ImageBrush m_badgeImageBrush;

	/// <summary>
	/// XAML Element for the first Ellipse matching x:Name of BadgingBackgroundEllipse.
	/// </summary>
	Ellipse m_badgingEllipse;

	/// <summary>
	/// XAML Element for the first Ellipse matching x:Name of BadgingEllipse.
	/// </summary>
	Ellipse m_badgingBackgroundEllipse;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
	/// <summary>
	/// The async operation object representing the loading and assignment of the Thumbnail.
	/// </summary>
	IAsyncOperation<IRandomAccessStreamWithContentType> m_profilePictureReadAsync;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null

	/// <summary>
	/// The initials from the DisplayName property.
	/// </summary>
	string m_displayNameInitials = string.Empty; // Uno docs: hstring in C++ is a struct and can't be null. So we explicitly initialize to string.Empty.

	/// <summary>
	/// The initials from the Contact property.
	/// </summary>
	string m_contactDisplayNameInitials = string.Empty; // Uno docs: hstring in C++ is a struct and can't be null. So we explicitly initialize to string.Empty.

	/// <summary>
	/// The ImageSource from the Contact property.
	/// </summary>
	ImageSource m_contactImageSource;

	public PersonPicture()
	{
		DefaultStyleKey = typeof(PersonPicture);

		TemplateSettings = new PersonPictureTemplateSettings();

		Unloaded += OnUnloaded;
		SizeChanged += OnSizeChanged;
	}

#if false
	Task<BitmapImage> LoadImageAsync(IRandomAccessStreamReference thumbStreamReference)
	{
		m_profilePictureReadAsync = null;

		// Contact is not yet supported.
		throw new NotSupportedException("Contact is not yet supported");

		//var operation = thumbStreamReference.OpenReadAsync();

		//operation.Completed(
		//	AsyncOperationCompletedHandler<IRandomAccessStreamWithContentType>(
		//		[strongThis, completedFunction](
		//			IAsyncOperation<IRandomAccessStreamWithContentType> operation,
		//			AsyncStatus asyncStatus)
		//{
		//	strongThis.DispatcherQueue().TryEnqueue(new DispatcherQueueHandler(
		//		[strongThis, asyncStatus, completedFunction, operation]()
		//	{
		//		BitmapImage bitmap;

		//		// Handle the failure case here to ensure we are on the UI thread.
		//		if (asyncStatus != AsyncStatus.Completed)
		//		{
		//			strongThis.m_profilePictureReadAsync = null);
		//			return;
		//		}

		//		try
		//		{
		//			bitmap.SetSourceAsync(operation.GetResults()).Completed(
		//				AsyncActionCompletedHandler(
		//					[strongThis, completedFunction, bitmap](IAsyncAction, AsyncStatus asyncStatus)
		//			{
		//				if (asyncStatus != AsyncStatus.Completed)
		//				{
		//					strongThis.m_profilePictureReadAsync = null);
		//					return;
		//				}

		//				completedFunction(bitmap);
		//				strongThis.m_profilePictureReadAsync = null);
		//			}));
		//		}
		//		catch (hresult_error &e)
		//		{
		//			strongThis.m_profilePictureReadAsync = null);

		//			// Ignore the exception if the image is invalid
		//			if (e.to_abi() == E_INVALIDARG)
		//			{
		//				return;
		//			}
		//			else
		//			{
		//				throw;
		//			}
		//		}
		//	}));
		//}));

		//m_profilePictureReadAsync = operation;
	}
#endif

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new Microsoft.UI.Xaml.Automation.Peers.PersonPictureAutomationPeer(this);
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		// Retrieve pointers to stable controls
		m_initialsTextBlock = GetTemplateChild("InitialsTextBlock") as TextBlock;

		m_badgeNumberTextBlock = GetTemplateChild("BadgeNumberTextBlock") as TextBlock;
		m_badgeGlyphIcon = GetTemplateChild("BadgeGlyphIcon") as FontIcon;
		m_badgingEllipse = GetTemplateChild("BadgingEllipse") as Ellipse;
		m_badgingBackgroundEllipse = GetTemplateChild("BadgingBackgroundEllipse") as Ellipse;

		UpdateBadge();
		UpdateIfReady();
	}

	string GetInitials()
	{
		if (!string.IsNullOrEmpty(Initials))
		{
			return Initials;
		}
		else if (!string.IsNullOrEmpty(m_displayNameInitials))
		{
			return m_displayNameInitials;
		}
		else
		{
			return m_contactDisplayNameInitials;
		}
	}

	ImageSource GetImageSource()
	{
		if (ProfilePicture != null)
		{
			return ProfilePicture;
		}
		else
		{
			return m_contactImageSource;
		}
	}

	void UpdateIfReady()
	{
		string initials = GetInitials();
		ImageSource imageSrc = GetImageSource();

		var templateSettings = TemplateSettings;
		templateSettings.ActualInitials = initials;

		if (imageSrc is not null)
		{
			if (templateSettings.ActualImageBrush is ImageBrush imageBrush)
			{
				imageBrush.ImageSource = imageSrc;
			}
			else
			{
				templateSettings.ActualImageBrush = new ImageBrush()
				{
					ImageSource = imageSrc,
					Stretch = Stretch.UniformToFill
				};
			}
		}
		else
		{
			templateSettings.ActualImageBrush = null;
		}

#if __APPLE_UIKIT__
		if (templateSettings.ActualImageBrush is ImageBrush brush)
		{
			brush.ImageOpened += RefreshPhoto;
		}
#endif
		// If the control is converted to 'Group-mode', we'll clear individual-specific information.
		// When IsGroup evaluates to false, we will restore state.
		if (IsGroup)
		{
			VisualStateManager.GoToState(this, "Group", false);
		}
		else
		{
			if (imageSrc is not null
#if __APPLE_UIKIT__
			&& imageSrc.IsOpened
#endif
			)
			{
				VisualStateManager.GoToState(this, "Photo", false);
			}
			else if (!string.IsNullOrEmpty(initials))
			{
				VisualStateManager.GoToState(this, "Initials", false);
			}
			else
			{
				VisualStateManager.GoToState(this, "NoPhotoOrInitials", false);
			}
		}

		UpdateAutomationName();
	}

#if __APPLE_UIKIT__
	void RefreshPhoto(object sender, RoutedEventArgs e)
	{
		VisualStateManager.GoToState(this, "Photo", false);

		if (TemplateSettings.ActualImageBrush is { } brush)
		{
			brush.ImageOpened -= RefreshPhoto;
		}
	}
#endif

	void UpdateBadge()
	{
		if (BadgeImageSource != null)
		{
			UpdateBadgeImageSource();
		}
		else if (BadgeNumber != 0)
		{
			UpdateBadgeNumber();
		}
		else if (!string.IsNullOrEmpty(BadgeGlyph))
		{
			UpdateBadgeGlyph();
		}
		// No badge properties set, so clear the badge XAML
		else
		{
			VisualStateManager.GoToState(this, "NoBadge", false);

			if (m_badgeNumberTextBlock != null)
			{
				m_badgeNumberTextBlock.Text = "";
			}

			if (m_badgeGlyphIcon != null)
			{
				m_badgeGlyphIcon.Glyph = "";
			}
		}

		UpdateAutomationName();
	}

	void UpdateBadgeNumber()
	{
		if (m_badgingEllipse == null || m_badgeNumberTextBlock == null)
		{
			return;
		}

		int badgeNumber = BadgeNumber;

		if (badgeNumber <= 0)
		{
			VisualStateManager.GoToState(this, "NoBadge", false);
			m_badgeNumberTextBlock.Text = "";
			return;
		}

		// should have badging number to show if we are here
		VisualStateManager.GoToState(this, "BadgeWithoutImageSource", false);

		if (badgeNumber <= 99)
		{
			m_badgeNumberTextBlock.Text = badgeNumber.ToString(CultureInfo.CurrentCulture);
		}
		else
		{
			m_badgeNumberTextBlock.Text = "99+";
		}
	}

	void UpdateBadgeGlyph()
	{
		if (m_badgingEllipse == null || m_badgeGlyphIcon == null)
		{
			return;
		}

		string badgeGlyph = BadgeGlyph;

		if (string.IsNullOrEmpty(badgeGlyph))
		{
			VisualStateManager.GoToState(this, "NoBadge", false);
			m_badgeGlyphIcon.Glyph = "";
			return;
		}

		// should have badging Glyph to show if we are here
		VisualStateManager.GoToState(this, "BadgeWithoutImageSource", false);

		m_badgeGlyphIcon.Glyph = badgeGlyph;
	}

	void UpdateBadgeImageSource()
	{
		if (m_badgeImageBrush == null)
		{
			m_badgeImageBrush = GetTemplateChild("BadgeImageBrush") as ImageBrush;
		}

		if (m_badgingEllipse == null || m_badgeImageBrush == null)
		{
			return;
		}

		m_badgeImageBrush.ImageSource = BadgeImageSource;

		if (BadgeImageSource != null)
		{
			VisualStateManager.GoToState(this, "BadgeWithImageSource", false);
		}
		else
		{
			VisualStateManager.GoToState(this, "NoBadge", false);
		}
	}

	void UpdateAutomationName()
	{
		Contact contact = Contact;
		string automationName;
		string contactName;

		// The AutomationName for the control is in the format: PersonName, BadgeInformation.
		// PersonName is set based on the name / initial properties in the order below.
		// if none exist, it defaults to "Person"
		if (IsGroup)
		{
			contactName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_GroupName);
		}
		else if (contact != null && !string.IsNullOrEmpty(contact.DisplayName))
		{
			contactName = contact.DisplayName;
		}
		else if (!string.IsNullOrEmpty(DisplayName))
		{
			contactName = DisplayName;
		}
		else if (!string.IsNullOrEmpty(Initials))
		{
			contactName = Initials;
		}
		else
		{
			contactName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PersonName);
		}

		// BadgeInformation portion of the AutomationName is set to 'n items' if there is a BadgeNumber,
		// or 'icon' for BadgeGlyph or BadgeImageSource. If BadgeText is specified, it will override
		// the string 'items' or 'icon'
		if (BadgeNumber > 0)
		{
			if (!string.IsNullOrEmpty(BadgeText))
			{
				automationName = StringUtil.FormatString(
					ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_BadgeItemTextOverride),
					contactName,
					BadgeNumber,
					BadgeText);
			}
			else
			{
				automationName = StringUtil.FormatString(
					GetLocalizedPluralBadgeItemStringResource(BadgeNumber),
					contactName,
					BadgeNumber);
			}
		}
		else if (!string.IsNullOrEmpty(BadgeGlyph) || BadgeImageSource != null)
		{
			if (!string.IsNullOrEmpty(BadgeText))
			{
				automationName = StringUtil.FormatString(
					ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_BadgeIconTextOverride),
					contactName,
					BadgeText);
			}
			else
			{
				automationName = StringUtil.FormatString(
					ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_BadgeIcon),
					contactName);
			}
		}
		else
		{
			automationName = contactName;
		}

		AutomationProperties.SetName(this, automationName);
	}

	string GetLocalizedPluralBadgeItemStringResource(int numericValue)
	{
		int valueMod10 = numericValue % 10;
		string value;

		if (numericValue == 1)  // Singular
		{
			value = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_BadgeItemSingular);
		}
		else if (numericValue == 2) // 2
		{
			value = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_BadgeItemPlural7);
		}
		else if (numericValue == 3 || numericValue == 4) // 3,4
		{
			value = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_BadgeItemPlural2);
		}
		else if (numericValue >= 5 && numericValue <= 10) // 5-10
		{
			value = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_BadgeItemPlural5);
		}
		else if (numericValue >= 11 && numericValue <= 19) // 11-19
		{
			value = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_BadgeItemPlural6);
		}
		else if (valueMod10 == 1) // 21, 31, 41, etc.
		{
			value = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_BadgeItemPlural1);
		}
		else if (valueMod10 >= 2 && valueMod10 <= 4) // 22-24, 32-34, 42-44, etc.
		{
			value = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_BadgeItemPlural3);
		}
		else // Everything else... 0, 20, 25-30, 35-40, etc.
		{
			value = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_BadgeItemPlural4);
		}

		return value;
	}

	void UpdateControlForContact(bool isNewContact)
	{
		Contact contact = Contact;

		if (contact == null)
		{
			// Explicitly setting to empty/null ensures the bound XAML is
			// correctly updated.
			m_contactDisplayNameInitials = "";
			m_contactImageSource = null;
			UpdateIfReady();
			return;
		}

		// It's possible for a second update to occur before the first finished loading
		// a profile picture (regardless of second having a picture or not).
		// Cancellation of any previously-activated tasks will mitigate race conditions.
		if (m_profilePictureReadAsync != null)
		{
			m_profilePictureReadAsync.Cancel();
		}

		m_contactDisplayNameInitials = InitialsGenerator.InitialsFromContactObject(contact);

		// Order of preference (but all work): Large, Small, Source, Thumbnail
		IRandomAccessStreamReference thumbStreamReference = null;

		if (PreferSmallImage && contact.SmallDisplayPicture != null)
		{
			thumbStreamReference = contact.SmallDisplayPicture;
		}
		else
		{
			if (contact.LargeDisplayPicture != null)
			{
				thumbStreamReference = contact.LargeDisplayPicture;
			}
			else if (contact.SmallDisplayPicture != null)
			{
				thumbStreamReference = contact.SmallDisplayPicture;
			}
			else if (contact.SourceDisplayPicture != null)
			{
				thumbStreamReference = contact.SourceDisplayPicture;
			}
			else if (contact.Thumbnail != null)
			{
				thumbStreamReference = contact.Thumbnail;
			}
		}

		// If we have profile picture data available per the above, async load the picture from the platform.
		if (thumbStreamReference != null)
		{
			if (isNewContact)
			{
				// Prevent the case where context of a contact changes, but we show an old person while the new one is loaded.
				m_contactImageSource = null;
			}

			// The dispatcher is not available in design mode, so when in design mode bypass the call to LoadImageAsync.
			if (!SharedHelpers.IsInDesignMode())
			{
				// Contact is not yet supported
				throw new NotSupportedException("Contact is not yet supported");

				//LoadImageAsync(
				//	thumbStreamReference,
				//	[strongThis](BitmapImage profileBitmap)
				//{
				//	profileBitmap.DecodePixelType(DecodePixelType.Logical);

				//	// We want to constrain the shorter side to the same dimension as the control, allowing the decoder to
				//	// choose the other dimension without distorting the image.
				//	if (profileBitmap.PixelHeight() < profileBitmap.PixelWidth())
				//	{
				//		profileBitmap.DecodePixelHeight((int)(strongThis.Height()));
				//	}
				//	else
				//	{
				//		profileBitmap.DecodePixelWidth((int)(strongThis.Width()));
				//	}

				//	strongThis.m_contactImageSource = ImageSource(profileBitmap));
				//	strongThis.UpdateIfReady();
				//});
			}
		}
		else
		{
			// Else clause indicates that (Contact.Thumbnail == null).
			m_contactImageSource = null;
		}

		UpdateIfReady();
	}

	void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		DependencyProperty property = args.Property;

		if (property == BadgeNumberProperty ||
			property == BadgeGlyphProperty ||
			property == BadgeImageSourceProperty)
		{
			UpdateBadge();
		}
		else if (property == BadgeTextProperty)
		{
			UpdateAutomationName();
		}
		else if (property == ContactProperty)
		{
			OnContactChanged(args);
		}
		else if (property == DisplayNameProperty)
		{
			OnDisplayNameChanged(args);
		}
		else if (property == ProfilePictureProperty ||
			property == InitialsProperty ||
			property == IsGroupProperty)
		{
			UpdateIfReady();
		}
		// No additional action required for s_PreferSmallImageProperty
	}

	void OnDisplayNameChanged(DependencyPropertyChangedEventArgs args)
	{
		m_displayNameInitials = InitialsGenerator.InitialsFromDisplayName(DisplayName);

		UpdateIfReady();
	}

	void OnContactChanged(DependencyPropertyChangedEventArgs args)
	{
		bool isNewContact = true;

		if (args != null && args.OldValue != null && args.NewValue != null)
		{
			Contact oldContact = args.OldValue as Contact;
			Contact newContact = args.NewValue as Contact;

			// Verify that the IDs are not null before comparing the old and new contact object.
			// If both contact IDs are null, it will be treated as a newcontact.
			if (!string.IsNullOrWhiteSpace(oldContact.Id) || !string.IsNullOrWhiteSpace(newContact.Id))
			{
				isNewContact = oldContact.Id != newContact.Id;
			}
		}

		UpdateControlForContact(isNewContact);
	}

	void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		{
			bool widthChanged = (args.NewSize.Width != args.PreviousSize.Width);
			bool heightChanged = (args.NewSize.Height != args.PreviousSize.Height);
			double newSize;

			if (widthChanged && heightChanged)
			{
				// Maintain circle by enforcing the new size on both Width and Height.
				// To do so, we will use the minimum value.
				newSize = (args.NewSize.Width < args.NewSize.Height) ? args.NewSize.Width : args.NewSize.Height;
			}
			else if (widthChanged)
			{
				newSize = args.NewSize.Width;
			}
			else if (heightChanged)
			{
				newSize = args.NewSize.Height;
			}
			else
			{
				return;
			}

			Height = newSize;
			Width = newSize;
		}

		// Calculate the FontSize of the control's text. Design guidelines have specified the
		// font size to be 42% of the container. Since it's circular, 42% of either Width or Height.
		// Note that we cap it to a minimum of 1, since a font size of less than 1 is an invalid value
		// that will result in a failure.
		double fontSize = Math.Max(1.0, Width * .42);

		if (m_initialsTextBlock != null)
		{
			m_initialsTextBlock.FontSize = fontSize;
		}

		if (m_badgingEllipse != null && m_badgingBackgroundEllipse != null && m_badgeNumberTextBlock != null && m_badgeGlyphIcon != null)
		{
			// Maintain badging circle and font size by enforcing the new size on both Width and Height.
			// Design guidelines have specified the font size to be 60% of the badging plate, and we want to keep
			// badging plate to be about 50% of the control so that don't block the initial/profile picture.
			double newSize = (args.NewSize.Width < args.NewSize.Height) ? args.NewSize.Width : args.NewSize.Height;
			m_badgingEllipse.Height = newSize * 0.5;
			m_badgingEllipse.Width = newSize * 0.5;
			m_badgingBackgroundEllipse.Height = newSize * 0.5;
			m_badgingBackgroundEllipse.Width = newSize * 0.5;
			m_badgeNumberTextBlock.FontSize = Math.Max(1.0, m_badgingEllipse.Height * 0.6);
			m_badgeGlyphIcon.FontSize = Math.Max(1.0, m_badgingEllipse.Height * 0.6);
		}
	}

	void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (m_profilePictureReadAsync != null)
		{
			m_profilePictureReadAsync.Cancel();
		}
	}
}
