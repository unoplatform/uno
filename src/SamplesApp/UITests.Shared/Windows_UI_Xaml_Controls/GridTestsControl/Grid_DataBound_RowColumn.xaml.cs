using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Helper;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Uno.UI.Samples.Content.UITests.GridTestsControl
{
	[Sample("Grid", "Grid_DataBound_RowColumn", IgnoreInSnapshotTests: true)]
	public sealed partial class Grid_DataBound_RowColumn : UserControl
	{
		public Grid_DataBound_RowColumn()
		{
			this.InitializeComponent();

			var controlData = new Grid_DataBound_RowColumn_Data();
			DataContext = controlData;

			this.RunWhileLoaded(
				async ct =>
				{
					var border = new Border();
					border.Background = new SolidColorBrush(Colors.Yellow);
					border.SetBinding(Grid.RowProperty, new Microsoft.UI.Xaml.Data.Binding { Path = new PropertyPath("CurrentRow") });
					border.SetBinding(Grid.ColumnProperty, new Microsoft.UI.Xaml.Data.Binding { Path = new PropertyPath("CurrentColumn") });

					for (int i = 0; !ct.IsCancellationRequested; i++)
					{
						var panel = (Panel)FindName("InnerGrid");

						if (panel != null)
						{
							controlData.CurrentColumn = i % 3;
							controlData.CurrentRow = i % 5;

							if ((i % 3) == 0)
							{
								if (border.Parent == null)
								{
									panel.Children.Add(border);
								}
								else
								{
									panel.Children.Remove(border);
								}
							}
						}

						await Task.Delay(1000);
					}
				}
			);
		}
	}

	public partial class Grid_DataBound_RowColumn_Data : DependencyObject
	{
		public int CurrentColumn
		{
			get => (int)this.GetValue(CurrentColumnProperty);
			set => this.SetValue(CurrentColumnProperty, value);
		}

		// Using a DependencyProperty as the backing store for CurrentColumn.  This enables animation, styling, binding, etc...
		public static DependencyProperty CurrentColumnProperty { get; } =
			DependencyProperty.Register("CurrentColumn", typeof(int), typeof(Grid_DataBound_RowColumn_Data), new PropertyMetadata(0));

		public int CurrentRow
		{
			get => (int)this.GetValue(CurrentRowProperty);
			set => this.SetValue(CurrentRowProperty, value);
		}

		// Using a DependencyProperty as the backing store for CurrentColumn.  This enables animation, styling, binding, etc...
		public static DependencyProperty CurrentRowProperty { get; } =
			DependencyProperty.Register("CurrentRow", typeof(int), typeof(Grid_DataBound_RowColumn_Data), new PropertyMetadata(0));

	}
}
