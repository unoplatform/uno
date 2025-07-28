using Microsoft.UI.Xaml.Controls;

namespace FrameworkPoolEditorRecycling.Editors;

public sealed partial class EditorBindingView : Page
{
	public EditorViewModel ViewModel { get; private set; }

	public EditorBindingView()
	{
		this.InitializeComponent();
		this.DataContextChanged += EditorBindingView_DataContextChanged;
	}

	private void EditorBindingView_DataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
	{
		if (DataContext is EditorViewModel editor)
		{
			ViewModel = editor;
		}
	}
}
