// MUX reference InfoBar.properties.cpp, tag winui3/release/1.4.2

#nullable enable

using System.Windows.Input;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

public partial class InfoBar
{
	/// <summary>
	/// Occurs after the close button is clicked in the InfoBar.
	/// </summary>
	public event TypedEventHandler<InfoBar, object> CloseButtonClick;

	/// <summary>
	/// Occurs just before the InfoBar begins to close.
	/// </summary>
	public event TypedEventHandler<InfoBar, InfoBarClosingEventArgs> Closing;

	/// <summary>
	/// Occurs after the InfoBar is closed.
	/// </summary>
	public event TypedEventHandler<InfoBar, InfoBarClosedEventArgs> Closed;

	/// <summary>
	/// Gets or sets the action button of the InfoBar.
	/// </summary>
	public ButtonBase ActionButton
	{
		get => (ButtonBase)GetValue(ActionButtonProperty);
		set => SetValue(ActionButtonProperty, value);
	}

	/// <summary>
	/// Identifies the ActionButton dependency property.
	/// </summary>
	public static DependencyProperty ActionButtonProperty { get; } =
		DependencyProperty.Register(nameof(ActionButton), typeof(ButtonBase), typeof(InfoBar), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the command to invoke when the close button is clicked in the InfoBar.
	/// </summary>
	public ICommand CloseButtonCommand
	{
		get => (ICommand)GetValue(CloseButtonCommandProperty);
		set => SetValue(CloseButtonCommandProperty, value);
	}

	/// <summary>
	/// Gets or sets the parameter to pass to the command for the close button in the InfoBar.
	/// </summary>
	public static DependencyProperty CloseButtonCommandProperty { get; } =
		DependencyProperty.Register(nameof(CloseButtonCommand), typeof(ICommand), typeof(InfoBar), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Identifies the CloseButtonCommandParameter dependency property.
	/// </summary>
	public object CloseButtonCommandParameter
	{
		get => (object)GetValue(CloseButtonCommandParameterProperty);
		set => SetValue(CloseButtonCommandParameterProperty, value);
	}

	/// <summary>
	/// Identifies the CloseButtonCommand dependency property.
	/// </summary>
	public static DependencyProperty CloseButtonCommandParameterProperty { get; } =
		DependencyProperty.Register(nameof(CloseButtonCommandParameter), typeof(object), typeof(InfoBar), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the Style to apply to the close button in the InfoBar.
	/// </summary>
	public Style CloseButtonStyle
	{
		get => (Style)GetValue(CloseButtonStyleProperty);
		set => SetValue(CloseButtonStyleProperty, value);
	}

	/// <summary>
	/// Gets or sets the Style to apply to the close button in the InfoBar.
	/// </summary>
	public static DependencyProperty CloseButtonStyleProperty { get; } =
		DependencyProperty.Register(nameof(CloseButtonStyle), typeof(Style), typeof(InfoBar), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Gets or sets the XAML Content that is displayed below the title and message in the InfoBar.
	/// </summary>
	public object Content
	{
		get => (object)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	/// <summary>
	/// Identifies the Content dependency property.
	/// </summary>
	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(nameof(Content), typeof(object), typeof(InfoBar), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the data template for the InfoBar.Content.
	/// </summary>
	public DataTemplate ContentTemplate
	{
		get => (DataTemplate)GetValue(ContentTemplateProperty);
		set => SetValue(ContentTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the ContentTemplate dependency property.
	/// </summary>
	public static DependencyProperty ContentTemplateProperty { get; } =
		DependencyProperty.Register(nameof(ContentTemplate), typeof(DataTemplate), typeof(InfoBar), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Gets or sets the graphic content to appear alongside the title and message in the InfoBar.
	/// </summary>
	public IconSource IconSource
	{
		get => (IconSource)GetValue(IconSourceProperty);
		set => SetValue(IconSourceProperty, value);
	}

	/// <summary>
	/// Identifies the IconSource dependency property.
	/// </summary>
	public static DependencyProperty IconSourceProperty { get; } =
		DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(InfoBar), new FrameworkPropertyMetadata(null, OnIconSourcePropertyChanged));

	/// <summary>
	/// Identifies the IconSource dependency property.
	/// </summary>
	public bool IsClosable
	{
		get => (bool)GetValue(IsClosableProperty);
		set => SetValue(IsClosableProperty, value);
	}

	/// <summary>
	/// Identifies the IsClosable dependency property.
	/// </summary>
	public static DependencyProperty IsClosableProperty { get; } =
		DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(InfoBar), new FrameworkPropertyMetadata(true, OnIsClosablePropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the icon is visible in the InfoBar.
	/// </summary>
	public bool IsIconVisible
	{
		get => (bool)GetValue(IsIconVisibleProperty);
		set => SetValue(IsIconVisibleProperty, value);
	}

	/// <summary>
	/// Identifies the IsIconVisible dependency property.
	/// </summary>
	public static DependencyProperty IsIconVisibleProperty { get; } =
		DependencyProperty.Register(nameof(IsIconVisible), typeof(bool), typeof(InfoBar), new FrameworkPropertyMetadata(true, OnIsIconVisiblePropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the InfoBar is open.
	/// </summary>
	public bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	/// <summary>
	/// Identifies the IsOpen dependency property.
	/// </summary>
	public static DependencyProperty IsOpenProperty { get; } =
		DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(InfoBar), new FrameworkPropertyMetadata(false, OnIsOpenPropertyChanged));

	/// <summary>
	/// Gets or sets the message of the InfoBar.
	/// </summary>
	public string Message
	{
		get => (string)GetValue(MessageProperty);
		set => SetValue(MessageProperty, value);
	}

	/// <summary>
	/// Identifies the Message dependency property.
	/// </summary>
	public static DependencyProperty MessageProperty { get; } =
		DependencyProperty.Register(nameof(Message), typeof(string), typeof(InfoBar), new FrameworkPropertyMetadata(string.Empty));

	/// <summary>
	/// Gets or sets the type of the InfoBar to apply consistent status color, icon,
	/// and assistive technology settings dependent on the criticality of the notification.
	/// </summary>
	public InfoBarSeverity Severity
	{
		get => (InfoBarSeverity)GetValue(SeverityProperty);
		set => SetValue(SeverityProperty, value);
	}

	/// <summary>
	/// Identifies the Severity dependency property.
	/// </summary>
	public static DependencyProperty SeverityProperty { get; } =
		DependencyProperty.Register(nameof(Severity), typeof(InfoBarSeverity), typeof(InfoBar), new FrameworkPropertyMetadata(InfoBarSeverity.Informational, OnSeverityPropertyChanged));

	/// <summary>
	/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for an InfoBar.
	/// Not intended for general use.
	/// </summary>
	public InfoBarTemplateSettings TemplateSettings => (InfoBarTemplateSettings)GetValue(TemplateSettingsProperty);

	/// <summary>
	/// Identifies the TemplateSettings dependency property.
	/// </summary>
	public static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(nameof(TemplateSettings), typeof(InfoBarTemplateSettings), typeof(InfoBar), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the title of the InfoBar.
	/// </summary>
	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	/// <summary>
	/// Identifies the Title dependency property.
	/// </summary>
	public static DependencyProperty TitleProperty { get; } =
		DependencyProperty.Register(nameof(Title), typeof(string), typeof(InfoBar), new FrameworkPropertyMetadata(string.Empty));

	private static void OnIconSourcePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (InfoBar)sender;
		owner.OnIconSourcePropertyChanged(args);
	}

	private static void OnIsClosablePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (InfoBar)sender;
		owner.OnIsClosablePropertyChanged(args);
	}

	private static void OnIsIconVisiblePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (InfoBar)sender;
		owner.OnIsIconVisiblePropertyChanged(args);
	}

	private static void OnIsOpenPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (InfoBar)sender;
		owner.OnIsOpenPropertyChanged(args);
	}

	private static void OnSeverityPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (InfoBar)sender;
		owner.OnSeverityPropertyChanged(args);
	}
}
