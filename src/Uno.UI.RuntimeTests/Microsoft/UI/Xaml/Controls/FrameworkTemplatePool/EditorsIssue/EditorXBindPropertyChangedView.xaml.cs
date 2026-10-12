// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

using Microsoft.UI.Xaml.Controls;

namespace FrameworkPoolEditorRecycling.Editors;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class EditorXBindPropertyChangedView : Page
{
	public EditorViewModel ViewModel { get; private set; }

	public EditorXBindPropertyChangedView()
	{
		this.InitializeComponent();
		this.DataContextChanged += Editor2View_DataContextChanged;
	}

	private void Editor2View_DataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
	{
		if (DataContext is EditorViewModel editor)
		{
			ViewModel = editor;
			Bindings.Update();
		}
	}
}
