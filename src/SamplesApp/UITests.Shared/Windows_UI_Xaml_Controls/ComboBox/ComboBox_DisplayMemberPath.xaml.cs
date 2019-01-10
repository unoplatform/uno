using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox
{
	[SampleControlInfo("ComboBox", "ComboBox_DisplayMemberPath")]
	public sealed partial class ComboBox_DisplayMemberPath : UserControl
	{
		public ComboBox_DisplayMemberPath()
		{
			this.InitializeComponent();

			DataContext = new MyEntity[]
			{
				new MyEntity { Name = "First" },
				new MyEntity { Name = "Second" },
				new MyEntity { Name = "Third" },
				new MyEntity { Name = "Fourth" },
				new MyEntity { Name = "Fifth" },
				new MyEntity { Name = "Sixth" },
				new MyEntity { Name = "Seventh" },
			};
		}

		public class MyEntity
		{
			public string Name { get; set; }

			// We're using DisplayMemberPath="Name", therefore we shouldn't see MyEntity.ToString().
			public override string ToString() => "If you see this text, there's a bug.";
		}
	}
}
