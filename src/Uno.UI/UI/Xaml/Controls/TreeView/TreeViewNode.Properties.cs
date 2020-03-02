using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	public partial class TreeViewNode
	{
		public object Content
		{
			get { return (object)GetValue(ContentProperty); }
			set { SetValue(ContentProperty, value); }
		}
				
		public int Depth
		{
			get { return (int)GetValue(DepthProperty); }
			private set { SetValue(DepthProperty, value); }
		}		

		public bool HasChildren
		{
			get { return (bool)GetValue(HasChildrenProperty); }
			private set { SetValue(HasChildrenProperty, value); }
		}

		public bool IsExpanded
		{
			get { return (bool)GetValue(IsExpandedProperty); }
			set { SetValue(IsExpandedProperty, value); }
		}

		public static readonly DependencyProperty ContentProperty =
			DependencyProperty.Register(nameof(Content), typeof(object), typeof(TreeViewNode), new PropertyMetadata(null));
		
		public static readonly DependencyProperty DepthProperty =
			DependencyProperty.Register(nameof(Depth), typeof(int), typeof(TreeViewNode), new PropertyMetadata(-1));

		public static readonly DependencyProperty HasChildrenProperty =
			DependencyProperty.Register(nameof(HasChildren), typeof(bool), typeof(TreeViewNode), new PropertyMetadata(false));
			   
		public static readonly DependencyProperty IsExpandedProperty =
			DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(TreeViewNode), new PropertyMetadata(false));
	}
}
