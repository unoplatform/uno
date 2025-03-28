using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView")]
	public sealed partial class ListView_DisplayMemberPath : Page
	{
		public ListView_DisplayMemberPath()
		{
			this.InitializeComponent();

			list.Items.Add(new { Name = "Name 1", Value = 1 });
			list.Items.Add(new { Name = "Name 2", Value = 2 });
			list.Items.Add(new { Name = "Name 3", Value = 3 });
			list.Items.Add(new { Name = "Name 4", Value = 4 });

			list.DisplayMemberPath = "Name";
		}
	}
}
