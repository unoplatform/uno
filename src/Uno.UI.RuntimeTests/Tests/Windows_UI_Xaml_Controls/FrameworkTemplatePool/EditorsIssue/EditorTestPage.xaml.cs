using System.Linq;
using Windows.UI.Xaml.Controls;

namespace FrameworkPoolEditorRecycling
{
	public sealed partial class EditorTestPage : Page
	{
		public EditorTestViewModel ViewModel { get; private set; }

		public EditorTestPage()
		{
			ViewModel = new EditorTestViewModel();

			this.InitializeComponent();
		}
	}
}
