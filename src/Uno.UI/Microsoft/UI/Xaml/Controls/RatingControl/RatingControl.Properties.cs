using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class RatingControl
	{
		public string Caption
		{
			get => (string)GetValue(CaptionProperty);
			set => SetValue(CaptionProperty, value);
		}

		public static DependencyProperty CaptionProperty { get; } =
			DependencyProperty.Register(nameof(Caption), typeof(string), typeof(RatingControl), new PropertyMetadata(default(string), OnCaptionPropertyChanged));

		public int InitialSetValue
		{
			get => (int)GetValue(InitialSetValueProperty);
			set => SetValue(InitialSetValueProperty, value);
		}

		public static DependencyProperty InitialSetValueProperty { get; } =
			DependencyProperty.Register(nameof(InitialSetValue), typeof(int), typeof(RatingControl), new PropertyMetadata(1, OnInitialSetValuePropertyChanged));

		public bool IsClearEnabled
		{
			get => (bool)GetValue(IsClearEnabledProperty);
			set => SetValue(IsClearEnabledProperty, value);
		}

		public static DependencyProperty IsClearEnabledProperty { get; } =
			DependencyProperty.Register(nameof(IsClearEnabled), typeof(bool), typeof(RatingControl), new PropertyMetadata(true, OnIsClearEnabledPropertyChanged));

		public bool IsReadOnly
		{
			get => (bool)GetValue(IsReadOnlyProperty);
			set => SetValue(IsReadOnlyProperty, value);
		}

		public static DependencyProperty IsReadOnlyProperty { get; } =
			DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(RatingControl), new PropertyMetadata(default(bool), OnIsReadOnlyPropertyChanged));

		public RatingItemInfo ItemInfo
		{
			get => (RatingItemInfo)GetValue(ItemInfoProperty);
			set => SetValue(ItemInfoProperty, value);
		}

		public static DependencyProperty ItemInfoProperty { get; } =
			DependencyProperty.Register(nameof(ItemInfo), typeof(RatingItemInfo), typeof(RatingControl), new PropertyMetadata(null, OnItemInfoPropertyChanged));

		public int MaxRating
		{
			get => (int)GetValue(MaxRatingProperty);
			set => SetValue(MaxRatingProperty, value);
		}

		public static DependencyProperty MaxRatingProperty { get; } =
			DependencyProperty.Register(nameof(MaxRating), typeof(int), typeof(RatingControl), new PropertyMetadata(5, OnMaxRatingPropertyChanged));

		public double PlaceholderValue
		{
			get => (double)GetValue(PlaceholderValueProperty);
			set => SetValue(PlaceholderValueProperty, value);
		}

		public static DependencyProperty PlaceholderValueProperty { get; } =
			DependencyProperty.Register(nameof(PlaceholderValue), typeof(double), typeof(RatingControl), new PropertyMetadata(-1.0, OnPlaceholderValuePropertyChanged));

		public double Value
		{
			get => (double)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public static DependencyProperty ValueProperty { get; } =
			DependencyProperty.Register(nameof(Value), typeof(double), typeof(RatingControl), new PropertyMetadata(-1.0, OnValuePropertyChanged));


		private static void OnCaptionPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as RatingControl;
			owner.OnPropertyChanged(args);
		}

		private static void OnInitialSetValuePropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as RatingControl;
			owner.OnPropertyChanged(args);
		}

		private static void OnIsClearEnabledPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as RatingControl;
			owner.OnPropertyChanged(args);
		}

		private static void OnIsReadOnlyPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as RatingControl;
			owner.OnPropertyChanged(args);
		}

		private static void OnItemInfoPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as RatingControl;
			owner.OnPropertyChanged(args);
		}

		private static void OnMaxRatingPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as RatingControl;
			owner.OnPropertyChanged(args);
		}

		private static void OnPlaceholderValuePropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as RatingControl;
			owner.OnPropertyChanged(args);
		}

		private static void OnValuePropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as RatingControl;
			owner.OnPropertyChanged(args);
		}
	}
}
