using System.Linq;
using Windows.UI.Xaml.Controls;

namespace MobileTemplateSelectorIssue
{
	public sealed partial class EditorTestPage : Page
	{
		public EditorTestViewModel ViewModel { get; private set; }

		public EditorTestPage()
		{
			ViewModel = new EditorTestViewModel();

			this.InitializeComponent();
		}

		public void SwitchEditors()
		{
			ViewModel.CurrentEditor = ViewModel.Editors.FirstOrDefault(e => e != ViewModel.CurrentEditor);
		}
	}
}
