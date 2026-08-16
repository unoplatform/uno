using System.Linq;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[Sample("Buttons", IsManualTest = true)]
	public sealed partial class ComboBox_ReopenDropdown : UserControl
	{
		public ComboBox_ReopenDropdown()
		{
			this.InitializeComponent();
			cb.ItemsSource = Enumerable.Range(0, 20000).ToArray();
		}
	}
}
