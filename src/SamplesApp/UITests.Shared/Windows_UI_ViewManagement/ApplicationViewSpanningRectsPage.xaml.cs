using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_ViewManagement
{
	[SampleControlInfo(category: "Windows.UI.ViewManagement")]
	public sealed partial class ApplicationViewSpanningRectsPage : Page
	{
		public ApplicationViewSpanningRectsPage()
		{
			this.InitializeComponent();

			SizeChanged += async (snd, e) =>
			{
				await Task.Delay(1200);
				RecalculateRects();
			};

			Loaded += (snd, e) => RecalculateRects();
		}

		private void RecalculateRects()
		{
#if __ANDROID__
			var transform = canvas.TransformToVisual(Window.Current.Content);
			var canvasOrigin = transform.TransformPoint(new Point());

			rect1.Width = 0;
			rect1.Height = 0;
			rect2.Width = 0;
			rect2.Height = 0;

			var spanningRects = ApplicationView.GetForCurrentView().GetSpanningRects();
			if (spanningRects == null || spanningRects.Count == 0)
			{
				return;
			}

			var canvasRect = new Rect(canvasOrigin, canvas.AssignedActualSize);

			var displayRects = spanningRects
				.Select(r => r.PhysicalToLogicalPixels().IntersectWith(canvasRect))
				.Where(r => r != null)
				.Select(r => ((Rect)r))
				.ToArray();

			if (displayRects.Length > 0)
			{
				Canvas.SetTop(rect1, displayRects[0].Top);
				Canvas.SetLeft(rect1, displayRects[0].Left);
				rect1.Width = displayRects[0].Width;
				rect1.Height = displayRects[0].Height;
			}
			if (displayRects.Length > 1)
			{
				Canvas.SetTop(rect2, displayRects[1].Top);
				Canvas.SetLeft(rect2, displayRects[1].Left);
				rect2.Width = displayRects[1].Width;
				rect2.Height = displayRects[1].Height;
			}
#endif
		}
	}
}
