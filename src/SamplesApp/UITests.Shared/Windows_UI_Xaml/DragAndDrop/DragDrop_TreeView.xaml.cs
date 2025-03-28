
using Windows.UI.Xaml.Controls;
using TreeView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeView;
using TreeViewNode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewNode;
using Uno.UI.Samples.Controls;


namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	[Sample("DragAndDrop", "TreeView")]
	public sealed partial class DragDrop_TreeView : Page
	{
		public DragDrop_TreeView()
		{
			this.InitializeComponent();
			bt0.Click += (s, e) =>
			{
				tv.RootNodes.Clear();
				tv.RootNodes.Add(new TreeViewNode
				{
					Content = "aaa",
					Children =
					{
						new TreeViewNode
						{
							Content = "bbb"
						}
					},
					IsExpanded = true

				});
				tv.RootNodes.Add(new TreeViewNode
				{
					Content = "ccc"
				});
				tv.RootNodes.Add(new TreeViewNode
				{
					Content = "ddd"
				});
				tv.RootNodes.Add(new TreeViewNode
				{
					Content = "eee"
				});
			};

			//"ddd" to "aaa"
			bt1.Click += (s, e) =>
			{
				tv.RootNodes.Clear();
				tv.RootNodes.Add(new TreeViewNode
				{
					Content = "aaa",
					Children =
					{
						new TreeViewNode
						{
							Content = "bbb"
						},
						new TreeViewNode
						{
							Content = "ddd"
						}
					},
					IsExpanded = true

				});
				tv.RootNodes.Add(new TreeViewNode { Content = "ccc" });
				tv.RootNodes.Add(new TreeViewNode { Content = "eee" });
			};

			// "bbb" to "eee"
			bt2.Click += (s, e) =>
			{
				tv.RootNodes.Clear();
				tv.RootNodes.Add(new TreeViewNode
				{
					Content = "aaa",
					Children =
					{
						new TreeViewNode
						{
							Content = "ddd"
						}
					},
					IsExpanded = true,
				});
				tv.RootNodes.Add(new TreeViewNode { Content = "ccc" });
				tv.RootNodes.Add(new TreeViewNode
				{
					Content = "eee",
					Children =
					{
						new TreeViewNode { Content = "bbb" },
					},
					IsExpanded = false,
				});
			};

			//"eee" to between "aaa" and "ddd"
			bt3.Click += (s, e) =>
			{
				tv.RootNodes.Clear();
				tv.RootNodes.Add(new TreeViewNode
				{
					Content = "aaa",
					Children =
					{
						new TreeViewNode
						{
							Content = "eee",
							Children =
							{
								new TreeViewNode { Content = "bbb" },
							},
							IsExpanded = false,
						},
						new TreeViewNode { Content = "ddd" }
					},
					IsExpanded = true,
				});
				tv.RootNodes.Add(new TreeViewNode
				{
					Content = "ccc",
				});
			};

			tv.DragItemsCompleted += (s, e) =>
			{
				tb.Text = "DragItemsCompleted is triggered";
			};

			radio_disable.Checked += (s, e) =>
			{
				tb.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			};

			radio_enable.Checked += (s, e) =>
			{
				tb.Visibility = Windows.UI.Xaml.Visibility.Visible;
			};

			radio_disable.IsChecked = true;
		}
	}
}
