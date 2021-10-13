#nullable enable

using Windows.Foundation;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class RefreshVisualizer
	{
		public UIElement? Content
		{
			get => (UIElement)GetValue(ContentProperty);
			set => SetValue(ContentProperty, value);
		}

		public static DependencyProperty ContentProperty { get; } =
			DependencyProperty.Register(nameof(Content), typeof(UIElement), typeof(RefreshVisualizer), new FrameworkPropertyMetadata(null, OnPropertyChanged));

		public static DependencyProperty InfoProviderProperty { get; } =
			DependencyProperty.Register(nameof(InfoProvider), typeof(object), typeof(RefreshVisualizer), new FrameworkPropertyMetadata(null, OnPropertyChanged));

		public RefreshVisualizerOrientation Orientation
		{
			get => (RefreshVisualizerOrientation)GetValue(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}

		public static DependencyProperty OrientationProperty { get; } =
			DependencyProperty.Register(nameof(Orientation), typeof(RefreshVisualizerOrientation), typeof(RefreshVisualizer), new FrameworkPropertyMetadata(RefreshVisualizerOrientation.Auto, OnPropertyChanged));

		public RefreshVisualizerState State
		{
			get => (RefreshVisualizerState)GetValue(StateProperty);
			private set => SetValue(StateProperty, value);
		}

		public static DependencyProperty StateProperty { get; } =
			DependencyProperty.Register(nameof(State), typeof(RefreshVisualizerState), typeof(RefreshVisualizer), new FrameworkPropertyMetadata(RefreshVisualizerState.Idle, OnPropertyChanged));

		private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (RefreshVisualizer)sender;
			owner.OnPropertyChanged(args);
		}

		/// <summary>
		/// Occurs when an update of the content has been initiated.
		/// </summary>
		public TypedEventHandler<RefreshVisualizer, RefreshRequestedEventArgs>? RefreshRequested;

		/// <summary>
		/// Occurs when an update of the content has been initiated.
		/// </summary>
		public TypedEventHandler<RefreshVisualizer, RefreshStateChangedEventArgs>? RefreshStateChanged;
	}
}
