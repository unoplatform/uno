using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

public sealed partial class ElementName_CodeAddedSource : UserControl
{
	public ElementName_CodeAddedSource(bool addSourceInLoaded)
	{
		this.InitializeComponent();

		if (addSourceInLoaded)
		{
			Loaded += (s, e) => AddSource();
		}
		else
		{
			AddSource();
		}
	}

	private void AddSource()
	{
		if (ParentSP.Children.Count == 1)
		{
			ParentSP.Children.Add(new TextBlock { Name = "SourceTextBlock", Text = "Hello, World!" });
		}
	}
}
