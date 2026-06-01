using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI;

public sealed partial class ResourceMarkupTest_Setup : Page
{
	public ResourceMarkupTest_Setup()
	{
		this.Loading += (s, e) =>
		{
		};
		this.InitializeComponent();
	}

	private void DebugVT(object sender, RoutedEventArgs e)
	{
		var tree = (RootPanel as StackPanel).TreeGraph(DebugVTN);

		static IEnumerable<string> DebugVTN(object x)
		{
			if (x is ContentControl { Content: ITestable b })
			{
				yield return $"Content=[{b.GetType().Name}]{b.Text}";
			}
			if (x is FrameworkElement { Tag: ITestable a })
			{
				yield return $"Tag=[{a.GetType().Name}]{a.Text}";
			}
		}
	}
}
