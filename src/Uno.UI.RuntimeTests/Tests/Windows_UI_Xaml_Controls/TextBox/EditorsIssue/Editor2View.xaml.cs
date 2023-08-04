// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

using Windows.UI.Xaml.Controls;

namespace MobileTemplateSelectorIssue.Editors
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Editor2View : Page
	{
		public Editor2 ViewModel { get; private set; }

		public Editor2View()
		{
			this.InitializeComponent();
			this.DataContextChanged += Editor2View_DataContextChanged;
		}

		private void Editor2View_DataContextChanged(Windows.UI.Xaml.FrameworkElement sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
		{
			if (DataContext is Editor2 editor)
			{
				ViewModel = editor;
			}
		}
	}
}
