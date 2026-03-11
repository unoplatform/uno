using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	///
	[Sample("ListView", Name = nameof(ListView_Resizing), ViewModelType = typeof(ListViewViewModel), Description = "ListView with adding and removing items afer initialization", IgnoreInSnapshotTests = true)]
	public sealed partial class ListView_Resizing : UserControl
	{
		public ListView_Resizing()
		{
			this.InitializeComponent();
		}
	}
}
