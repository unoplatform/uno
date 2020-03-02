using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls
{
    public partial class TreeViewCollapsedEventArgs
    {
		internal TreeViewCollapsedEventArgs(TreeViewNode node)
		{
			Node = node;
		}

		public TreeViewNode Node { get; }

		public object Item => Node.Content;
	}
}
