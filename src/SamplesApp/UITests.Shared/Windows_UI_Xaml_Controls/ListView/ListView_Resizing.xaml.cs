using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	///
	[SampleControlInfo("ListView", nameof(ListView_Resizing), typeof(ListViewViewModel), description: "ListView with adding and removing items afer initialization", ignoreInSnapshotTests: true)]
	public sealed partial class ListView_Resizing : UserControl
	{
		public ListView_Resizing()
		{
			this.InitializeComponent();
		}
	}
}
