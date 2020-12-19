using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PipsPager
    {
		public Style DefaultIndicatorButtonStyle
		{
			get => (Style)GetValue(DefaultIndicatorButtonStyleProperty);
			set => SetValue(DefaultIndicatorButtonStyleProperty, value);
		}

		public static DependencyProperty DefaultIndicatorButtonStyleProperty { get; } =
			DependencyProperty.Register(nameof(DefaultIndicatorButtonStyle), typeof(Style), typeof(PipsPager), new PropertyMetadata(null));

		public int MaxVisualIndicators
		{
			get => (int)GetValue(MaxVisualIndicatorsProperty);
			set => SetValue(MaxVisualIndicatorsProperty, value);
		}

		public static DependencyProperty MaxVisualIndicatorsProperty { get; } =
			DependencyProperty.Register(nameof(MaxVisualIndicators), typeof(int), typeof(PipsPager), new PropertyMetadata(0));

		public Style NextButtonStyle
		{
			get => (Style)GetValue(NextButtonStyleProperty);
			set => SetValue(NextButtonStyleProperty, value);
		}

		public static DependencyProperty NextButtonStyleProperty { get; } =
			DependencyProperty.Register(nameof(NextButtonStyle), typeof(Style), typeof(PipsPager), new PropertyMetadata(0);

		public PipsPagerButtonVisibility NextButtonVisibility
		{
			get => (PipsPagerButtonVisibility)GetValue(NextButtonVisibilityProperty);
			set => SetValue(NextButtonVisibilityProperty, value);
		}

		public static DependencyProperty NextButtonVisibilityProperty { get; } =
			DependencyProperty.Register(nameof(NextButtonVisibility), typeof(PipsPagerButtonVisibility), typeof(PipsPager), new PropertyMetadata(PipsPagerButtonVisibility.Collapsed));

		public int NumberOfPages
		{
			get => (int)GetValue(NumberOfPagesProperty);
			set => SetValue(NumberOfPagesProperty, value);
		}

		public static DependencyProperty NumberOfPagesProperty { get; } =
			DependencyProperty.Register(nameof(NumberOfPages), typeof(int), typeof(PipsPager), new PropertyMetadata(-1));

		public Orientation Orientation
		{
			get => (Orientation)GetValue(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}

		public static DependencyProperty OrientationProperty { get; } =
			DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(PipsPager), new PropertyMetadata(Orientation.Horizontal));

		public Style PreviousButtonStyle
		{
			get => (Style)GetValue(PreviousButtonStyleProperty);
			set => SetValue(PreviousButtonStyleProperty, value);
		}

		public static DependencyProperty PreviousButtonStyleProperty { get; } =
			DependencyProperty.Register(nameof(PreviousButtonStyle), typeof(Style), typeof(PipsPager), new PropertyMetadata(null));

		public PipsPagerButtonVisibility PreviousButtonVisibility
		{
			get => (PipsPagerButtonVisibility)GetValue(PreviousButtonVisibilityProperty);
			set => SetValue(PreviousButtonVisibilityProperty, value);
		}

		public static DependencyProperty PreviousButtonVisibilityProperty { get; } =
			DependencyProperty.Register(nameof(PreviousButtonVisibility), typeof(PipsPagerButtonVisibility), typeof(PipsPager), new PropertyMetadata(PipsPagerButtonVisibility.Collapsed));

		public Style SelectedIndicatorButtonStyle
		{
			get => (Style)GetValue(SelectedIndicatorButtonStyleProperty);
			set => SetValue(SelectedIndicatorButtonStyleProperty, value);
		}

		public static DependencyProperty SelectedIndicatorButtonStyleProperty { get; } =
			DependencyProperty.Register(nameof(SelectedIndicatorButtonStyle), typeof(Style), typeof(PipsPager), new PropertyMetadata(null));

		public int SelectedPageIndex
		{
			get => (int)GetValue(SelectedPageIndexProperty);
			set => SetValue(SelectedPageIndexProperty, value);
		}

		public static DependencyProperty SelectedPageIndexProperty { get; } =
			DependencyProperty.Register(nameof(SelectedPageIndex), typeof(int), typeof(PipsPager), new PropertyMetadata(0));

		public PipsPagerTemplateSettings TemplateSettings
		{
			get => (PipsPagerTemplateSettings)GetValue(TemplateSettingsProperty);
			set => SetValue(TemplateSettingsProperty, value);
		}

		public static DependencyProperty TemplateSettingsProperty { get; } =
			DependencyProperty.Register(nameof(TemplateSettings), typeof(PipsPagerTemplateSettings), typeof(PipsPager), new PropertyMetadata(null));

		private static void OnPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = (PipsPager)sender;
			owner.OnPropertyChanged(args);
		}

		// Events

		public event TypedEventHandler<PipsPager, PipsPagerSelectedIndexChangedEventArgs> SelectedIndexChanged;
	}
}
