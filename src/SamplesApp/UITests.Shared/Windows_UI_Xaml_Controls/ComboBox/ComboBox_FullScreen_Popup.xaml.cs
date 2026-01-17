using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Uno.UI.Samples.Controls;
using System.Linq;

namespace SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample("ComboBox")]
	public sealed partial class ComboBox_FullScreen_Popup : UserControl
	{
		public ComboBox_FullScreen_Popup()
		{
			this.InitializeComponent();
		}

		public string[] MyItems { get; } = Enumerable.Range(0, 10).Select(i => $"Item {i}").ToArray();
	}
}
