using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	public sealed partial class Binding_Event_NamedElement : Page
	{
		public Binding_Event_NamedElement()
		{
			this.InitializeComponent();
		}
	}

	public partial class Binding_Event_NamedElement_Target : Border
	{
		public int CheckedRaised { get; private set; }

		public int UncheckedRaised { get; private set; }

		public void OnCheckedRaised() => CheckedRaised++;

		public void OnUncheckedRaised(object sender, RoutedEventArgs args) => UncheckedRaised++;
	}
}
