using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample("ComboBox", Name = "ComboBox_DisplayMemberPath")]
	public sealed partial class ComboBox_DisplayMemberPath : UserControl
	{
		public ComboBox_DisplayMemberPath()
		{
			this.InitializeComponent();

			DataContext = new MyEntity[]
			{
				new MyEntity("ID1", "First"),
				new MyEntity("ID2", "Second"),
				new MyEntity("ID3", "Third"),
				new MyEntity("ID4", "Fourth"),
				new MyEntity("ID5", "Fifth"),
				new MyEntity("ID6", "Sixth"),
				new MyEntity("ID7", "Seventh")
			};
		}

		public class MyEntity
		{
			public MyEntity(string id, string name)
			{
				Id = id;
				Name = name;
			}

			public string Id { get; }

			public string Name { get; }

			// We're using DisplayMemberPath="Name", therefore we shouldn't see MyEntity.ToString().
			public override string ToString() => "If you see this text, there's a bug.";
		}
	}
}
