using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Toolkit
{
	[Sample(IsManualTest = true, Description = "The red border shouldn't feel as if it's duplicated (especially from the top-left corner)")]
	public sealed partial class ElevatedView_BorderThickness : Page
	{
		public ElevatedView_BorderThickness()
		{
			this.InitializeComponent();
		}
	}
}
