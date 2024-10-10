// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PagerControl.properties.cpp, tag winui3/release/1.4.2

using System.Windows.Input;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class PagerControl
{
	public bool ButtonPanelAlwaysShowFirstLastPageIndex
	{
		get => (bool)GetValue(ButtonPanelAlwaysShowFirstLastPageIndexProperty);
		set => SetValue(ButtonPanelAlwaysShowFirstLastPageIndexProperty, value);
	}

	public static DependencyProperty ButtonPanelAlwaysShowFirstLastPageIndexProperty { get; } =
		DependencyProperty.Register(nameof(ButtonPanelAlwaysShowFirstLastPageIndex), typeof(bool), typeof(PagerControl), new FrameworkPropertyMetadata(true, OnPropertyChanged));

	public PagerControlDisplayMode DisplayMode
	{
		get => (PagerControlDisplayMode)GetValue(DisplayModeProperty);
		set => SetValue(DisplayModeProperty, value);
	}

	public static DependencyProperty DisplayModeProperty { get; } =
		DependencyProperty.Register(nameof(DisplayMode), typeof(PagerControlDisplayMode), typeof(PagerControl), new FrameworkPropertyMetadata(default(PagerControlDisplayMode), OnPropertyChanged));

	public ICommand FirstButtonCommand
	{
		get => (ICommand)GetValue(FirstButtonCommandProperty);
		set => SetValue(FirstButtonCommandProperty, value);
	}

	public static DependencyProperty FirstButtonCommandProperty { get; } =
		DependencyProperty.Register(nameof(FirstButtonCommand), typeof(ICommand), typeof(PagerControl), new FrameworkPropertyMetadata(null, OnPropertyChanged));
	public Style FirstButtonStyle
	{
		get => (Style)GetValue(FirstButtonStyleProperty);
		set => SetValue(FirstButtonStyleProperty, value);
	}

	public static DependencyProperty FirstButtonStyleProperty { get; } =
		DependencyProperty.Register(nameof(FirstButtonStyle), typeof(Style), typeof(PagerControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, OnPropertyChanged));
	public PagerControlButtonVisibility FirstButtonVisibility
	{
		get => (PagerControlButtonVisibility)GetValue(FirstButtonVisibilityProperty);
		set => SetValue(FirstButtonVisibilityProperty, value);
	}

	public static DependencyProperty FirstButtonVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(FirstButtonVisibility), typeof(PagerControlButtonVisibility), typeof(PagerControl), new FrameworkPropertyMetadata(default(PagerControlButtonVisibility), OnPropertyChanged));

	public ICommand LastButtonCommand
	{
		get => (ICommand)GetValue(LastButtonCommandProperty);
		set => SetValue(LastButtonCommandProperty, value);
	}

	public static DependencyProperty LastButtonCommandProperty { get; } =
		DependencyProperty.Register(nameof(LastButtonCommand), typeof(ICommand), typeof(PagerControl), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	public Style LastButtonStyle
	{
		get => (Style)GetValue(LastButtonStyleProperty);
		set => SetValue(LastButtonStyleProperty, value);
	}

	public static DependencyProperty LastButtonStyleProperty { get; } =
		DependencyProperty.Register(nameof(LastButtonStyle), typeof(Style), typeof(PagerControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, OnPropertyChanged));

	public PagerControlButtonVisibility LastButtonVisibility
	{
		get => (PagerControlButtonVisibility)GetValue(LastButtonVisibilityProperty);
		set => SetValue(LastButtonVisibilityProperty, value);
	}

	public static DependencyProperty LastButtonVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(LastButtonVisibility), typeof(PagerControlButtonVisibility), typeof(PagerControl), new FrameworkPropertyMetadata(default(PagerControlButtonVisibility), OnPropertyChanged));

	public ICommand NextButtonCommand
	{
		get => (ICommand)GetValue(NextButtonCommandProperty);
		set => SetValue(NextButtonCommandProperty, value);
	}

	public static DependencyProperty NextButtonCommandProperty { get; } =
		DependencyProperty.Register(nameof(NextButtonCommand), typeof(ICommand), typeof(PagerControl), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	public Style NextButtonStyle
	{
		get => (Style)GetValue(NextButtonStyleProperty);
		set => SetValue(NextButtonStyleProperty, value);
	}

	public static DependencyProperty NextButtonStyleProperty { get; } =
		DependencyProperty.Register(nameof(NextButtonStyle), typeof(Style), typeof(PagerControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, OnPropertyChanged));

	public PagerControlButtonVisibility NextButtonVisibility
	{
		get => (PagerControlButtonVisibility)GetValue(NextButtonVisibilityProperty);
		set => SetValue(NextButtonVisibilityProperty, value);
	}

	public static DependencyProperty NextButtonVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(NextButtonVisibility), typeof(PagerControlButtonVisibility), typeof(PagerControl), new FrameworkPropertyMetadata(default(PagerControlButtonVisibility), OnPropertyChanged));

	public int NumberOfPages
	{
		get => (int)GetValue(NumberOfPagesProperty);
		set => SetValue(NumberOfPagesProperty, value);
	}

	public static DependencyProperty NumberOfPagesProperty { get; } =
		DependencyProperty.Register(nameof(NumberOfPages), typeof(int), typeof(PagerControl), new FrameworkPropertyMetadata(0, OnPropertyChanged));

	public string PrefixText
	{
		get => (string)GetValue(PrefixTextProperty);
		set => SetValue(PrefixTextProperty, value);
	}

	public static DependencyProperty PrefixTextProperty { get; } =
		DependencyProperty.Register(nameof(PrefixText), typeof(string), typeof(PagerControl), new FrameworkPropertyMetadata(string.Empty, OnPropertyChanged));

	public ICommand PreviousButtonCommand
	{
		get => (ICommand)GetValue(PreviousButtonCommandProperty);
		set => SetValue(PreviousButtonCommandProperty, value);
	}

	public static DependencyProperty PreviousButtonCommandProperty { get; } =
		DependencyProperty.Register(nameof(PreviousButtonCommand), typeof(ICommand), typeof(PagerControl), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	public Style PreviousButtonStyle
	{
		get => (Style)GetValue(PreviousButtonStyleProperty);
		set => SetValue(PreviousButtonStyleProperty, value);
	}

	public static DependencyProperty PreviousButtonStyleProperty { get; } =
		DependencyProperty.Register(nameof(PreviousButtonStyle), typeof(Style), typeof(PagerControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, OnPropertyChanged));

	public PagerControlButtonVisibility PreviousButtonVisibility
	{
		get => (PagerControlButtonVisibility)GetValue(PreviousButtonVisibilityProperty);
		set => SetValue(PreviousButtonVisibilityProperty, value);
	}

	public static DependencyProperty PreviousButtonVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(PreviousButtonVisibility), typeof(PagerControlButtonVisibility), typeof(PagerControl), new FrameworkPropertyMetadata(default(PagerControlButtonVisibility), OnPropertyChanged));

	public int SelectedPageIndex
	{
		get => (int)GetValue(SelectedPageIndexProperty);
		set => SetValue(SelectedPageIndexProperty, value);
	}

	public static DependencyProperty SelectedPageIndexProperty { get; } =
		DependencyProperty.Register(nameof(SelectedPageIndex), typeof(int), typeof(PagerControl), new FrameworkPropertyMetadata(0, OnPropertyChanged));

	public string SuffixText
	{
		get => (string)GetValue(SuffixTextProperty);
		set => SetValue(SuffixTextProperty, value);
	}

	public static DependencyProperty SuffixTextProperty { get; } =
		DependencyProperty.Register(nameof(SuffixText), typeof(string), typeof(PagerControl), new FrameworkPropertyMetadata(string.Empty, OnPropertyChanged));

	public PagerControlTemplateSettings TemplateSettings
	{
		get => (PagerControlTemplateSettings)GetValue(TemplateSettingsProperty);
		set => SetValue(TemplateSettingsProperty, value);
	}

	public static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(nameof(TemplateSettings), typeof(PagerControlTemplateSettings), typeof(PagerControl), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (PagerControl)sender;
		owner.OnPropertyChanged(args);
	}

	public event TypedEventHandler<PagerControl, PagerControlSelectedIndexChangedEventArgs> SelectedIndexChanged;
}
