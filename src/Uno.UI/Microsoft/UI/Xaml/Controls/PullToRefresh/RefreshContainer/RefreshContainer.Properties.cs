using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class RefreshContainer
	{
		public RefreshPullDirection PullDirection
		{
			get => (RefreshPullDirection)GetValue(PullDirectionProperty);
			set => SetValue(PullDirectionProperty, value);
		}

		public static DependencyProperty PullDirectionProperty { get; } =
			DependencyProperty.Register(nameof(PullDirection), typeof(RefreshPullDirection), typeof(RefreshContainer), new FrameworkPropertyMetadata(RefreshPullDirection.TopToBottom, OnPropertyChanged));

		public RefreshVisualizer Visualizer
		{
			get => (RefreshVisualizer)GetValue(VisualizerProperty);
			set => SetValue(VisualizerProperty, value);
		}

		public static DependencyProperty VisualizerProperty { get; } =
			DependencyProperty.Register(nameof(Visualizer), typeof(RefreshVisualizer), typeof(RefreshContainer), new FrameworkPropertyMetadata(null, OnPropertyChanged));

		private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (RefreshContainer)sender;
			owner.OnPropertyChanged(args);
		}
	}
}
