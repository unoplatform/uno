using System.Linq;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[SampleControlInfo("Scrolling", "ScrollViewer_Transforms", description: "Apply transforms and ensure manipulations are still working smoothly.", ignoreInSnapshotTests: true)]
	public sealed partial class ScrollViewer_Transforms : Page
	{
		public ScrollViewer_Transforms()
		{
			this.InitializeComponent();

			var atoz = Enumerable
				.Range(65, 26)
				.Select(i => ((char)i).ToString())
				.ToArray();

			DataContext = atoz.Select(x => atoz.Select(y => $"{x}:{y}").ToArray()).ToArray();
		}
	}
}
