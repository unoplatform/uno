using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace UITests.Shared.Windows_UI_Xaml.UIElementTests
{
	[SampleControlInfo("UIElement", "TransformToVisual_Simple")]
	public sealed partial class TransformToVisual_Simple : UserControl
	{
		private Grid _outer;
		private Border _inner;
		private Rectangle _content;

		public TransformToVisual_Simple()
		{
			this.InitializeComponent();

			_content = new Rectangle();
			_outer = new Grid()
			{
				Width = 100,
				Height = 100,
				Background = new SolidColorBrush(Colors.Cornsilk)
			};
			_inner = new Border()
			{
				Width = 10,
				Height = 10,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Background = new SolidColorBrush(Colors.Red),
				Child = _content
			};

			_outer.Children.Add(_inner);

			root.Content = _outer;

			Loaded += OnControlLoaded;

			SizeChanged += OnControlLoaded;
		}

		private async void OnControlLoaded(object sender, RoutedEventArgs e)
		{
			await Task.Yield();
			var transform = _inner.TransformToVisual(_outer);
			var rect = transform.TransformBounds(LayoutInformation.GetLayoutSlot(_inner));

			result.Text = $"{rect}\n" +
				$"LayoutSlot-root:{LayoutInformation.GetLayoutSlot(root)}\n" +
				$"LayoutSlot-outer:{LayoutInformation.GetLayoutSlot(_outer)}\n" +
				$"LayoutSlot-inner:{LayoutInformation.GetLayoutSlot(_inner)}\n" +
				$"LayoutSlot-content:{LayoutInformation.GetLayoutSlot(_content)}";
		}
	}
}
