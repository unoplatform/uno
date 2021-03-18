// MUX reference InfoBar.properties.cpp, commit 3125489

#nullable enable

using System.Windows.Input;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class InfoBar
	{
		public event TypedEventHandler<InfoBar, object> CloseButtonClick;

		public event TypedEventHandler<InfoBar, InfoBarClosingEventArgs> Closing;

		public event TypedEventHandler<InfoBar, InfoBarClosedEventArgs> Closed;

		public ButtonBase ActionButton
		{
			get => (ButtonBase)GetValue(ActionButtonProperty);
			set => SetValue(ActionButtonProperty, value);
		}

		public static DependencyProperty ActionButtonProperty { get; } =
			DependencyProperty.Register(nameof(ActionButton), typeof(ButtonBase), typeof(InfoBar), new PropertyMetadata(null));

		public ICommand CloseButtonCommand
		{
			get => (ICommand)GetValue(CloseButtonCommandProperty);
			set => SetValue(CloseButtonCommandProperty, value);
		}

		public static DependencyProperty CloseButtonCommandProperty { get; } =
			DependencyProperty.Register(nameof(CloseButtonCommand), typeof(ICommand), typeof(InfoBar), new PropertyMetadata(null));

		public object CloseButtonCommandParameter
		{
			get => (object)GetValue(CloseButtonCommandParameterProperty);
			set => SetValue(CloseButtonCommandParameterProperty, value);
		}

		public static DependencyProperty CloseButtonCommandParameterProperty { get; } =
			DependencyProperty.Register(nameof(CloseButtonCommandParameter), typeof(object), typeof(InfoBar), new PropertyMetadata(null));

		public Style CloseButtonStyle
		{
			get => (Style)GetValue(CloseButtonStyleProperty);
			set => SetValue(CloseButtonStyleProperty, value);
		}

		public static DependencyProperty CloseButtonStyleProperty { get; } =
			DependencyProperty.Register(nameof(CloseButtonStyle), typeof(Style), typeof(InfoBar), new PropertyMetadata(null));

		public object Content
		{
			get => (object)GetValue(ContentProperty);
			set => SetValue(ContentProperty, value);
		}

		public static DependencyProperty ContentProperty { get; } =
			DependencyProperty.Register(nameof(Content), typeof(object), typeof(InfoBar), new PropertyMetadata(null));

		public DataTemplate ContentTemplate
		{
			get => (DataTemplate)GetValue(ContentTemplateProperty);
			set => SetValue(ContentTemplateProperty, value);
		}

		public static DependencyProperty ContentTemplateProperty { get; } =
			DependencyProperty.Register(nameof(ContentTemplate), typeof(DataTemplate), typeof(InfoBar), new PropertyMetadata(null));

		public IconSource IconSource
		{
			get => (IconSource)GetValue(IconSourceProperty);
			set => SetValue(IconSourceProperty, value);
		}

		public static DependencyProperty IconSourceProperty { get; } =
			DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(InfoBar), new PropertyMetadata(null, OnIconSourcePropertyChanged));

		public bool IsClosable
		{
			get => (bool)GetValue(IsClosableProperty);
			set => SetValue(IsClosableProperty, value);
		}

		public static DependencyProperty IsClosableProperty { get; } =
			DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(InfoBar), new PropertyMetadata(true, OnIsClosablePropertyChanged));

		public bool IsIconVisible
		{
			get => (bool)GetValue(IsIconVisibleProperty);
			set => SetValue(IsIconVisibleProperty, value);
		}

		public static DependencyProperty IsIconVisibleProperty { get; } =
			DependencyProperty.Register(nameof(IsIconVisible), typeof(bool), typeof(InfoBar), new PropertyMetadata(true, OnIsIconVisiblePropertyChanged));

		public bool IsOpen
		{
			get => (bool)GetValue(IsOpenProperty);
			set => SetValue(IsOpenProperty, value);
		}

		public static DependencyProperty IsOpenProperty { get; } =
			DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(InfoBar), new PropertyMetadata(false, OnIsOpenPropertyChanged));

		public string Message
		{
			get => (string)GetValue(MessageProperty);
			set => SetValue(MessageProperty, value);
		}

		public static DependencyProperty MessageProperty { get; } =
			DependencyProperty.Register(nameof(Message), typeof(string), typeof(InfoBar), new PropertyMetadata(null));

		public InfoBarSeverity Severity
		{
			get => (InfoBarSeverity)GetValue(SeverityProperty);
			set => SetValue(SeverityProperty, value);
		}

		public static DependencyProperty SeverityProperty { get; } =
			DependencyProperty.Register(nameof(Severity), typeof(InfoBarSeverity), typeof(InfoBar), new PropertyMetadata(InfoBarSeverity.Informational, OnSeverityPropertyChanged));

		public InfoBarTemplateSettings TemplateSettings
		{
			get => (InfoBarTemplateSettings)GetValue(TemplateSettingsProperty);
			set => SetValue(TemplateSettingsProperty, value);
		}

		public static DependencyProperty TemplateSettingsProperty { get; } =
			DependencyProperty.Register(nameof(TemplateSettings), typeof(InfoBarTemplateSettings), typeof(InfoBar), new PropertyMetadata(null));

		public string Title
		{
			get => (string)GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}

		public static DependencyProperty TitleProperty { get; } =
			DependencyProperty.Register(nameof(Title), typeof(string), typeof(InfoBar), new PropertyMetadata(null));

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
}
