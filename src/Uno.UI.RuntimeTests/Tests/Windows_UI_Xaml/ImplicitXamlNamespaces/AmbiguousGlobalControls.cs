using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// Register two different CLR namespaces to the global URI, each containing
// a type named TestCard, to test ambiguity detection and disambiguation.
[assembly: System.Windows.Markup.XmlnsDefinition(
	"http://schemas.microsoft.com/winfx/2006/xaml/presentation/global",
	"Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.AmbiguousA")]
[assembly: System.Windows.Markup.XmlnsDefinition(
	"http://schemas.microsoft.com/winfx/2006/xaml/presentation/global",
	"Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.AmbiguousB")]

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.AmbiguousA
{
	/// <summary>
	/// A unique control in namespace A for disambiguation testing.
	/// </summary>
	public class UniqueControlA : ContentControl
	{
		public string LabelA
		{
			get => (string)GetValue(LabelAProperty);
			set => SetValue(LabelAProperty, value);
		}

		public static readonly DependencyProperty LabelAProperty =
			DependencyProperty.Register(nameof(LabelA), typeof(string), typeof(UniqueControlA),
				new PropertyMetadata("Default A"));
	}
}

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.AmbiguousB
{
	/// <summary>
	/// A unique control in namespace B for disambiguation testing.
	/// </summary>
	public class UniqueControlB : ContentControl
	{
		public string LabelB
		{
			get => (string)GetValue(LabelBProperty);
			set => SetValue(LabelBProperty, value);
		}

		public static readonly DependencyProperty LabelBProperty =
			DependencyProperty.Register(nameof(LabelB), typeof(string), typeof(UniqueControlB),
				new PropertyMetadata("Default B"));
	}
}
