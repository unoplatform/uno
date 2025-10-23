// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PersonPicture.properties.cpp, tag winui3/release/1.4.2

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Windows.ApplicationModel.Contacts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

public partial class PersonPicture : Control
{
	public ImageSource ProfilePicture
	{
		get => (ImageSource)GetValue(ProfilePictureProperty);
		set => SetValue(ProfilePictureProperty, value);
	}

	public bool PreferSmallImage
	{
		get => (bool)GetValue(PreferSmallImageProperty);
		set => SetValue(PreferSmallImageProperty, value);
	}

	public bool IsGroup
	{
		get => (bool)GetValue(IsGroupProperty);
		set => SetValue(IsGroupProperty, value);
	}

	public string Initials
	{
		get => (string)GetValue(InitialsProperty);
		set => SetValue(InitialsProperty, value);
	}

	public string DisplayName
	{
		get => (string)GetValue(DisplayNameProperty);
		set => SetValue(DisplayNameProperty, value);
	}

	public Contact Contact
	{
		get => (Contact)GetValue(ContactProperty);
		set => SetValue(ContactProperty, value);
	}

	public string BadgeText
	{
		get => (string)GetValue(BadgeTextProperty);
		set => SetValue(BadgeTextProperty, value);
	}

	public int BadgeNumber
	{
		get => (int)GetValue(BadgeNumberProperty);
		set => SetValue(BadgeNumberProperty, value);
	}

	public ImageSource BadgeImageSource
	{
		get => (ImageSource)GetValue(BadgeImageSourceProperty);
		set => SetValue(BadgeImageSourceProperty, value);
	}

	public string BadgeGlyph
	{
		get => (string)GetValue(BadgeGlyphProperty);
		set => SetValue(BadgeGlyphProperty, value);
	}

	public PersonPictureTemplateSettings TemplateSettings
	{
		get => (PersonPictureTemplateSettings)GetValue(TemplateSettingsProperty);
		internal set => SetValue(TemplateSettingsProperty, value);
	}

	public static DependencyProperty BadgeGlyphProperty { get; } =
		DependencyProperty.Register(
			nameof(BadgeGlyph),
			typeof(string),
			typeof(PersonPicture),
			new FrameworkPropertyMetadata("", OnPropertyChanged));

	public static DependencyProperty BadgeImageSourceProperty { get; } =
		DependencyProperty.Register(
			nameof(BadgeImageSource),
			typeof(ImageSource),
			typeof(PersonPicture),
			new FrameworkPropertyMetadata(default(ImageSource), OnPropertyChanged));

	public static DependencyProperty BadgeNumberProperty { get; } =
		DependencyProperty.Register(
			nameof(BadgeNumber),
			typeof(int),
			typeof(PersonPicture),
			new FrameworkPropertyMetadata(0, OnPropertyChanged));

	public static DependencyProperty BadgeTextProperty { get; } =
		DependencyProperty.Register(
			nameof(BadgeText),
			typeof(string),
			typeof(PersonPicture),
			new FrameworkPropertyMetadata(string.Empty, OnPropertyChanged));

	public static DependencyProperty ContactProperty { get; } =
		DependencyProperty.Register(
			nameof(Contact),
			typeof(Contact),
			typeof(PersonPicture),
			new FrameworkPropertyMetadata(default(Contact), OnPropertyChanged));

	public static DependencyProperty DisplayNameProperty { get; } =
		DependencyProperty.Register(
			nameof(DisplayName),
			typeof(string),
			typeof(PersonPicture),
			new FrameworkPropertyMetadata("", OnPropertyChanged));

	public static DependencyProperty InitialsProperty { get; } =
		DependencyProperty.Register(
			nameof(Initials),
			typeof(string),
			typeof(PersonPicture),
			new FrameworkPropertyMetadata("", OnPropertyChanged));

	public static DependencyProperty IsGroupProperty { get; } =
		DependencyProperty.Register(
			nameof(IsGroup),
			typeof(bool),
			typeof(PersonPicture),
			new FrameworkPropertyMetadata(default(bool), OnPropertyChanged));

	public static DependencyProperty PreferSmallImageProperty { get; } =
		DependencyProperty.Register(
			nameof(PreferSmallImage),
			typeof(bool),
			typeof(PersonPicture),
			new FrameworkPropertyMetadata(default(bool), OnPropertyChanged));

	public static DependencyProperty ProfilePictureProperty { get; } =
		DependencyProperty.Register(
			nameof(ProfilePicture),
			typeof(ImageSource),
			typeof(PersonPicture),
			new FrameworkPropertyMetadata(default(ImageSource), OnPropertyChanged));

	internal static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(
			nameof(TemplateSettings),
			typeof(PersonPictureTemplateSettings),
			typeof(PersonPicture),
			new FrameworkPropertyMetadata(default(PersonPictureTemplateSettings), OnPropertyChanged));

	private static void OnPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (PersonPicture)sender;
		owner.OnPropertyChanged(args);
	}
}
