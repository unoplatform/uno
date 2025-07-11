using System.Windows.Input;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

// TODO:MZ: Verify default values match WinUI
public partial class ContentDialog
{
	/// <summary>
	/// Gets or sets the command to invoke when the close button is tapped.
	/// </summary>
	public ICommand CloseButtonCommand
	{
		get => (ICommand)GetValue(CloseButtonCommandProperty);
		set => SetValue(CloseButtonCommandProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the CloseButtonCommand dependency property.
	/// </summary>
	public static DependencyProperty CloseButtonCommandProperty { get; } =
		DependencyProperty.Register(
			nameof(CloseButtonCommand),
			typeof(ICommand),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(default(ICommand)));

	/// <summary>
	/// Gets or sets the parameter to pass to the command for the close button.
	/// </summary>
	public object CloseButtonCommandParameter
	{
		get => GetValue(CloseButtonCommandParameterProperty);
		set => SetValue(CloseButtonCommandParameterProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the CloseButtonCommandParameter dependency property.
	/// </summary>
	public static DependencyProperty CloseButtonCommandParameterProperty { get; } =
		DependencyProperty.Register(
			nameof(CloseButtonCommandParameter),
			typeof(object),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(default(object)));

	/// <summary>
	/// Gets or sets the Style to apply to the dialog's close button.
	/// </summary>
	public Style CloseButtonStyle
	{
		get => (Style)GetValue(CloseButtonStyleProperty);
		set => SetValue(CloseButtonStyleProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the CloseButtonStyle dependency property.
	/// </summary>
	public static DependencyProperty CloseButtonStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(CloseButtonStyle),
			typeof(Style),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(
				default(Style),
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Gets or sets the text to display on the close button.
	/// </summary>
	public string CloseButtonText
	{
		get => (string)GetValue(CloseButtonTextProperty);
		set => SetValue(CloseButtonTextProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the CloseButtonText dependency property.
	/// </summary>
	public static DependencyProperty CloseButtonTextProperty { get; } =
		DependencyProperty.Register(
			nameof(CloseButtonText), typeof(string),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(
				defaultValue: ""));

	/// <summary>
	/// Gets or sets a value that indicates which button
	/// on the dialog is the default action.
	/// </summary>
	public ContentDialogButton DefaultButton
	{
		get => (ContentDialogButton)GetValue(DefaultButtonProperty);
		set => this.SetValue(DefaultButtonProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the DefaultButton dependency property.
	/// </summary>
	public static DependencyProperty DefaultButtonProperty { get; } =
		DependencyProperty.Register(
			nameof(DefaultButton),
			typeof(ContentDialogButton),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(ContentDialogButton.None));

	/// <summary>
	/// Gets or sets a value that indicates whether a request
	/// is being made to display the dialog full screen.
	/// </summary>
	public bool FullSizeDesired
	{
		get => (bool)GetValue(FullSizeDesiredProperty);
		set => SetValue(FullSizeDesiredProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the FullSizeDesired dependency property.
	/// </summary>
	public static DependencyProperty FullSizeDesiredProperty { get; } =
		DependencyProperty.Register(
			nameof(FullSizeDesired),
			typeof(bool),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(default(bool)));

	/// <summary>
	/// Gets or sets whether the dialog's primary button is enabled.
	/// </summary>
	public bool IsPrimaryButtonEnabled
	{
		get => (bool)GetValue(IsPrimaryButtonEnabledProperty);
		set => SetValue(IsPrimaryButtonEnabledProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the IsPrimaryButtonEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsPrimaryButtonEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsPrimaryButtonEnabled),
			typeof(bool),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets or sets whether the dialog's secondary button is enabled.
	/// </summary>
	public bool IsSecondaryButtonEnabled
	{
		get => (bool)GetValue(IsSecondaryButtonEnabledProperty);
		set => SetValue(IsSecondaryButtonEnabledProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the IsSecondaryButtonEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsSecondaryButtonEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsSecondaryButtonEnabled),
			typeof(bool),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets or sets the command to invoke when the primary button is tapped.
	/// </summary>
	public ICommand PrimaryButtonCommand
	{
		get => (ICommand)GetValue(PrimaryButtonCommandProperty);
		set => SetValue(PrimaryButtonCommandProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the PrimaryButtonCommand dependency property.
	/// </summary>
	public static DependencyProperty PrimaryButtonCommandProperty { get; } =
		DependencyProperty.Register(
			nameof(PrimaryButtonCommand),
			typeof(ICommand),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(default(ICommand)));

	/// <summary>
	/// Gets or sets the parameter to pass to the command for the primary button.
	/// </summary>
	public object PrimaryButtonCommandParameter
	{
		get => GetValue(PrimaryButtonCommandParameterProperty);
		set => SetValue(PrimaryButtonCommandParameterProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the PrimaryButtonCommandParameter dependency property.
	/// </summary>
	public static DependencyProperty PrimaryButtonCommandParameterProperty { get; } =
		DependencyProperty.Register(
			nameof(PrimaryButtonCommandParameter),
			typeof(object),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(default(object)));

	/// <summary>
	/// Gets or sets the Style to apply to the dialog's primary button.
	/// </summary>
	public Style PrimaryButtonStyle
	{
		get => (Style)GetValue(PrimaryButtonStyleProperty);
		set => SetValue(PrimaryButtonStyleProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the PrimaryButtonStyle dependency property.
	/// </summary>
	public static DependencyProperty PrimaryButtonStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(PrimaryButtonStyle),
			typeof(Style),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(
				default(Style),
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Gets or sets the text to display on the primary button.
	/// </summary>
	public string PrimaryButtonText
	{
		get => (string)GetValue(PrimaryButtonTextProperty);
		set => SetValue(PrimaryButtonTextProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the PrimaryButtonText dependency property.
	/// </summary>
	public static DependencyProperty PrimaryButtonTextProperty { get; } =
		DependencyProperty.Register(
			nameof(PrimaryButtonText),
			typeof(string),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(""));

	/// <summary>
	/// Gets or sets the command to invoke when the secondary button is tapped.
	/// </summary>
	public ICommand SecondaryButtonCommand
	{
		get => (ICommand)GetValue(SecondaryButtonCommandProperty);
		set => SetValue(SecondaryButtonCommandProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the SecondaryButtonCommand dependency property.
	/// </summary>
	public static DependencyProperty SecondaryButtonCommandProperty { get; } =
		DependencyProperty.Register(
			nameof(SecondaryButtonCommand),
			typeof(ICommand),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(default(ICommand)));

	/// <summary>
	/// Gets or sets the parameter to pass to the command for the secondary button.
	/// </summary>
	public object SecondaryButtonCommandParameter
	{
		get => GetValue(SecondaryButtonCommandParameterProperty);
		set => SetValue(SecondaryButtonCommandParameterProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the SecondaryButtonCommandParameter dependency property.
	/// </summary>
	public static DependencyProperty SecondaryButtonCommandParameterProperty { get; } =
		DependencyProperty.Register(
			nameof(SecondaryButtonCommandParameter),
			typeof(object),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(default(object)));

	/// <summary>
	/// Gets or sets the Style to apply to the dialog's secondary button.
	/// </summary>
	public Style SecondaryButtonStyle
	{
		get => (Style)GetValue(SecondaryButtonStyleProperty);
		set => SetValue(SecondaryButtonStyleProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the SecondaryButtonStyle dependency property.
	/// </summary>
	public static DependencyProperty SecondaryButtonStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(SecondaryButtonStyle),
			typeof(Style),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(
				default(Style),
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Gets or sets the text to be displayed on the secondary button.
	/// </summary>
	public string SecondaryButtonText
	{
		get => (string)GetValue(SecondaryButtonTextProperty);
		set => SetValue(SecondaryButtonTextProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the SecondaryButtonText dependency property.
	/// </summary>
	public static DependencyProperty SecondaryButtonTextProperty { get; } =
		DependencyProperty.Register(
			nameof(SecondaryButtonText),
			typeof(string),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(""));

	/// <summary>
	/// Gets or sets the title of the dialog.
	/// </summary>
	public object Title
	{
		get => GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the Title dependency property.
	/// </summary>
	public static DependencyProperty TitleProperty { get; } =
		DependencyProperty.Register(
			nameof(Title),
			typeof(object),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(default(object)));

	/// <summary>
	/// Gets or sets the title template.
	/// </summary>
	public DataTemplate TitleTemplate
	{
		get => (DataTemplate)GetValue(TitleTemplateProperty);
		set => SetValue(TitleTemplateProperty, value);
	}

	/// <summary>
	/// Gets the identifier of the TitleTemplate dependency property.
	/// </summary>
	public static DependencyProperty TitleTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(TitleTemplate),
			typeof(DataTemplate),
			typeof(ContentDialog),
			new FrameworkPropertyMetadata(
				default(DataTemplate),
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Occurs after the close button has been tapped.
	/// </summary>
	public event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> CloseButtonClick;

	/// <summary>
	/// Occurs after the dialog is closed.
	/// </summary>
	public event TypedEventHandler<ContentDialog, ContentDialogClosedEventArgs> Closed;

	/// <summary>
	/// Occurs after the dialog starts to close, but before
	/// it is closed and before the Closed event occurs.
	/// </summary>
	public event TypedEventHandler<ContentDialog, ContentDialogClosingEventArgs> Closing;

	/// <summary>
	/// Occurs after the dialog is opened.
	/// </summary>
	public event TypedEventHandler<ContentDialog, ContentDialogOpenedEventArgs> Opened;

	/// <summary>
	/// Occurs after the primary button has been tapped.
	/// </summary>
	public event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> PrimaryButtonClick;

	/// <summary>
	/// Occurs after the secondary button has been tapped.
	/// </summary>
	public event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> SecondaryButtonClick;
}
