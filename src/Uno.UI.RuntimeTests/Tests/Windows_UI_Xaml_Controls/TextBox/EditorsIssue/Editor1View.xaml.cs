using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MobileTemplateSelectorIssue.Editors
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Editor1View : Page
	{
		public Editor1 ViewModel { get; private set; }

		public Editor1View()
		{
			this.InitializeComponent();
			this.DataContextChanged += Editor1View_DataContextChanged;
		}

		private void Editor1View_DataContextChanged(Windows.UI.Xaml.FrameworkElement sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
		{
			if (DataContext is Editor1 editor)
			{
				ViewModel = editor;
			}
		}
	}
}
