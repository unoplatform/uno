using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data.ElementNameProbe;

public sealed partial class ENP_CodeAdded : UserControl
{
	public ENP_CodeAdded() : this(false)
	{
	}

	public ENP_CodeAdded(bool addInLoaded)
	{
		this.InitializeComponent();

		if (addInLoaded)
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
		if (ParentSP.Children.Count > 1)
		{
			return;
		}

		ParentSP.Children.Add(new TextBlock { Name = "SourceTextBlock", Text = "Hello, World!" });
	}
}
