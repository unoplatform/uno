using Microsoft.UI.Xaml.Controls;

namespace FrameworkPoolEditorRecycling.Editors;

public sealed partial class EditorBindingPropertyChangedView : Page
{
	public EditorViewModel ViewModel { get; private set; }

	public EditorBindingPropertyChangedView()
	{
		this.InitializeComponent();
		this.DataContextChanged += EditorBindingPropertyChangedView_DataContextChanged;
	}

	private void EditorBindingPropertyChangedView_DataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
	{
		if (DataContext is EditorViewModel editor)
		{
			ViewModel = editor;
		}
	}
}
