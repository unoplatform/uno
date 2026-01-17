using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;

namespace UITests.Shared.Windows_UI_Xaml.UIElementTests
{
	[Sample("UIElement", IgnoreInSnapshotTests: true)]
	public sealed partial class TransformToVisual_ScrollViewer : Page
	{
		private readonly TestRunner _tests;

		public TransformToVisual_ScrollViewer()
		{
			this.InitializeComponent();

			_tests = new TestRunner(this, TestsOutput);

			Loaded += RunTest;
		}

		private void RunTest(object sender, RoutedEventArgs e)
		{
			_tests.Run(
				() => When_VerticalScrollViewer_NotScrolled_Top(),
				() => When_VerticalScrollViewer_NotScrolled_Bottom(),
				() => When_VerticalScrollViewer_Scrolled_Top(),
				() => When_VerticalScrollViewer_Scrolled_Bottom(),
				() => When_HorizontalScrollViewer_NotScrolled_Left(),
				() => When_HorizontalScrollViewer_NotScrolled_Right(),
				() => When_HorizontalScrollViewer_Scrolled_Left(),
				() => When_HorizontalScrollViewer_Scrolled_Right()
			);
		}

		private const double _svExtent = 1000;
		private const double _svWidth = 100;
		private const double _svHeight = 100;

		public async Task When_VerticalScrollViewer_NotScrolled_Top()
		{
			VerticalScrollViewer.ChangeView(0, 0, null, disableAnimation: true);
			await Task.Delay(25);

			var sut = ScrollTop.TransformToVisual(VerticalScrollViewerParent);

			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.IsTrue(RectCloseComparer.UI.Equals(new Rect(0, 0, 50, 50), result));
		}

		public async Task When_VerticalScrollViewer_NotScrolled_Bottom()
		{
			VerticalScrollViewer.ChangeView(0, 0, null, disableAnimation: true);
			await Task.Delay(25);

			var sut = ScrollBottom.TransformToVisual(VerticalScrollViewerParent);

			var result = sut.TransformBounds(new Rect(0, ScrollBottom.ActualHeight, 50, 50));

			Assert.IsTrue(RectCloseComparer.UI.Equals(new Rect(0, _svExtent, 50, 50), result));
		}

		public async Task When_VerticalScrollViewer_Scrolled_Top()
		{
			var offset = _svExtent - _svHeight;
			VerticalScrollViewer.ChangeView(0, offset, null, disableAnimation: true);
			await Task.Delay(25);

			var sut = ScrollTop.TransformToVisual(VerticalScrollViewerParent);

			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.IsTrue(RectCloseComparer.UI.Equals(new Rect(0, -offset, 50, 50), result));
		}

		public async Task When_VerticalScrollViewer_Scrolled_Bottom()
		{
			var offset = _svExtent - _svHeight;
			VerticalScrollViewer.ChangeView(0, offset, null, disableAnimation: true);
			await Task.Delay(25);

			var sut = ScrollBottom.TransformToVisual(VerticalScrollViewerParent);

			var result = sut.TransformBounds(new Rect(0, ScrollBottom.ActualHeight, 50, 50));

			Assert.IsTrue(RectCloseComparer.UI.Equals(new Rect(0, _svHeight, 50, 50), result));
		}

		public async Task When_HorizontalScrollViewer_NotScrolled_Left()
		{
			HorizontalScrollViewer.ChangeView(0, 0, null, disableAnimation: true);
			await Task.Delay(25);

			var sut = ScrollLeft.TransformToVisual(HorizontalScrollViewerParent);

			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.IsTrue(RectCloseComparer.UI.Equals(new Rect(0, 0, 50, 50), result));
		}

		public async Task When_HorizontalScrollViewer_NotScrolled_Right()
		{
			HorizontalScrollViewer.ChangeView(0, 0, null, disableAnimation: true);
			await Task.Delay(25);

			var sut = ScrollRight.TransformToVisual(HorizontalScrollViewerParent);

			var result = sut.TransformBounds(new Rect(ScrollRight.ActualWidth, 0, 50, 50));

			Assert.IsTrue(RectCloseComparer.UI.Equals(new Rect(_svExtent, 0, 50, 50), result));
		}

		public async Task When_HorizontalScrollViewer_Scrolled_Left()
		{
			var offset = _svExtent - _svWidth;
			HorizontalScrollViewer.ChangeView(offset, 0, null, disableAnimation: true);
			await Task.Delay(25);

			var sut = ScrollLeft.TransformToVisual(HorizontalScrollViewerParent);

			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.IsTrue(RectCloseComparer.UI.Equals(new Rect(-offset, 0, 50, 50), result));
		}

		public async Task When_HorizontalScrollViewer_Scrolled_Right()
		{
			var offset = _svExtent - _svWidth;
			HorizontalScrollViewer.ChangeView(offset, 0, null, disableAnimation: true);
			await Task.Delay(25);

			var sut = ScrollRight.TransformToVisual(HorizontalScrollViewerParent);

			var result = sut.TransformBounds(new Rect(ScrollRight.ActualWidth, 0, 50, 50));

			Assert.IsTrue(RectCloseComparer.UI.Equals(new Rect(_svWidth, 0, 50, 50), result));
		}
	}
}
