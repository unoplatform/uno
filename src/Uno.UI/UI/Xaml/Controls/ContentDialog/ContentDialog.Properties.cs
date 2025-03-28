#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Windows.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentDialog : Controls.ContentControl
	{
		public DataTemplate TitleTemplate
		{
			get => (DataTemplate)GetValue(TitleTemplateProperty);
			set => SetValue(TitleTemplateProperty, value);
		}

		public object Title
		{
			get => (object)GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}

		public string SecondaryButtonText
		{
			get => (string)GetValue(SecondaryButtonTextProperty);
			set => SetValue(SecondaryButtonTextProperty, value);
		}

		public object SecondaryButtonCommandParameter
		{
			get => (object)GetValue(SecondaryButtonCommandParameterProperty);
			set => SetValue(SecondaryButtonCommandParameterProperty, value);
		}

		public ICommand SecondaryButtonCommand
		{
			get => (ICommand)GetValue(SecondaryButtonCommandProperty);
			set => SetValue(SecondaryButtonCommandProperty, value);
		}

		public string PrimaryButtonText
		{
			get => (string)GetValue(PrimaryButtonTextProperty);
			set => SetValue(PrimaryButtonTextProperty, value);
		}

		public object PrimaryButtonCommandParameter
		{
			get => (object)GetValue(PrimaryButtonCommandParameterProperty);
			set => SetValue(PrimaryButtonCommandParameterProperty, value);
		}

		public ICommand PrimaryButtonCommand
		{
			get => (ICommand)GetValue(PrimaryButtonCommandProperty);
			set => SetValue(PrimaryButtonCommandProperty, value);
		}

		public bool IsSecondaryButtonEnabled
		{
			get => (bool)GetValue(IsSecondaryButtonEnabledProperty);
			set => SetValue(IsSecondaryButtonEnabledProperty, value);
		}

		public bool IsPrimaryButtonEnabled
		{
			get => (bool)GetValue(IsPrimaryButtonEnabledProperty);
			set => SetValue(IsPrimaryButtonEnabledProperty, value);
		}

		public bool FullSizeDesired
		{
			get => (bool)GetValue(FullSizeDesiredProperty);
			set => SetValue(FullSizeDesiredProperty, value);
		}

		public Style SecondaryButtonStyle
		{
			get => (Style)GetValue(SecondaryButtonStyleProperty);
			set => SetValue(SecondaryButtonStyleProperty, value);
		}

		public Style PrimaryButtonStyle
		{
			get => (Style)GetValue(PrimaryButtonStyleProperty);
			set => SetValue(PrimaryButtonStyleProperty, value);
		}

		public Controls.ContentDialogButton DefaultButton
		{
			get => (Controls.ContentDialogButton)GetValue(DefaultButtonProperty);
			set => this.SetValue(DefaultButtonProperty, value);
		}

		public string CloseButtonText
		{
			get => (string)GetValue(CloseButtonTextProperty);
			set => SetValue(CloseButtonTextProperty, value);
		}

		public Style CloseButtonStyle
		{
			get => (Style)GetValue(CloseButtonStyleProperty);
			set => SetValue(CloseButtonStyleProperty, value);
		}

		public object CloseButtonCommandParameter
		{
			get => (object)GetValue(CloseButtonCommandParameterProperty);
			set => SetValue(CloseButtonCommandParameterProperty, value);
		}

		public ICommand CloseButtonCommand
		{
			get => (ICommand)GetValue(CloseButtonCommandProperty);
			set => SetValue(CloseButtonCommandProperty, value);
		}

		public static DependencyProperty FullSizeDesiredProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"FullSizeDesired", typeof(bool),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(default(bool), UpdateVisualState));

		private static void UpdateVisualState(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs newValue)
			=> (dependencyObject as ContentDialog).UpdateVisualState();

		public static DependencyProperty IsPrimaryButtonEnabledProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPrimaryButtonEnabled", typeof(bool),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(true));

		public static DependencyProperty IsSecondaryButtonEnabledProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsSecondaryButtonEnabled", typeof(bool),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(true));

		public static DependencyProperty PrimaryButtonCommandParameterProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PrimaryButtonCommandParameter", typeof(object),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(default(object)));

		public static DependencyProperty PrimaryButtonCommandProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PrimaryButtonCommand", typeof(ICommand),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(default(ICommand)));

		public static DependencyProperty PrimaryButtonTextProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PrimaryButtonText", typeof(string),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(
				defaultValue: "",
				propertyChangedCallback: UpdateVisualState));

		public static DependencyProperty SecondaryButtonCommandParameterProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"SecondaryButtonCommandParameter", typeof(object),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(default(object)));

		public static DependencyProperty SecondaryButtonCommandProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"SecondaryButtonCommand", typeof(ICommand),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(default(ICommand)));

		public static DependencyProperty SecondaryButtonTextProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"SecondaryButtonText", typeof(string),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(
				defaultValue: "",
				propertyChangedCallback: UpdateVisualState));

		public static DependencyProperty TitleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"Title", typeof(object),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(default(object), (PropertyChangedCallback)((o, e) => ((ContentDialog)o).UpdateTitleSpaceVisibility())));

		public static DependencyProperty TitleTemplateProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"TitleTemplate", typeof(DataTemplate),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(default(DataTemplate),
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,
				(PropertyChangedCallback)((o, e) => ((ContentDialog)o).UpdateTitleSpaceVisibility()))
			);

		public static DependencyProperty CloseButtonCommandParameterProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"CloseButtonCommandParameter", typeof(object),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(default(object)));

		public static DependencyProperty CloseButtonCommandProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"CloseButtonCommand", typeof(ICommand),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(default(ICommand)));

		public static DependencyProperty CloseButtonStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"CloseButtonStyle", typeof(Style),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(default(Style), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		public static DependencyProperty CloseButtonTextProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"CloseButtonText", typeof(string),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(
				defaultValue: "",
				propertyChangedCallback: UpdateVisualState));


		public static DependencyProperty DefaultButtonProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"DefaultButton", typeof(Controls.ContentDialogButton),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(
				defaultValue: ContentDialogButton.None,
				propertyChangedCallback: UpdateVisualState));

		public static DependencyProperty PrimaryButtonStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PrimaryButtonStyle", typeof(Style),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(default(Style), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		public static DependencyProperty SecondaryButtonStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"SecondaryButtonStyle", typeof(Style),
			typeof(Controls.ContentDialog),
			new FrameworkPropertyMetadata(default(Style), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));
	}
}
