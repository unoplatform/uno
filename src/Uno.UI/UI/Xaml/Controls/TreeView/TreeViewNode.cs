#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Controls
{
	[Uno.NotImplemented]
	public partial class TreeViewNode : DependencyObject
	{
		[Uno.NotImplemented]
		public bool IsExpanded
		{
			get
			{
				return (bool)this.GetValue(IsExpandedProperty);
			}
			set
			{
				this.SetValue(IsExpandedProperty, value);
			}
		}
		[Uno.NotImplemented]
		public bool HasUnrealizedChildren
		{
			get
			{
				throw new NotImplementedException("The member bool TreeViewNode.HasUnrealizedChildren is not implemented in Uno.");
			}
			set
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeViewNode", "bool TreeViewNode.HasUnrealizedChildren");
			}
		}

		[Uno.NotImplemented]
		public object Content
		{
			get
			{
				return (object)this.GetValue(ContentProperty);
			}
			set
			{
				this.SetValue(ContentProperty, value);
			}
		}

		[Uno.NotImplemented]
		public global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.TreeViewNode> Children
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<TreeViewNode> TreeViewNode.Children is not implemented in Uno.");
			}
		}

		[Uno.NotImplemented]
		public int Depth
		{
			get
			{
				return (int)this.GetValue(DepthProperty);
			}
		}

		[Uno.NotImplemented]
		public bool HasChildren
		{
			get
			{
				return (bool)this.GetValue(HasChildrenProperty);
			}
		}

		[Uno.NotImplemented]
		public TreeViewNode Parent
		{
			get
			{
				throw new NotImplementedException("The member TreeViewNode TreeViewNode.Parent is not implemented in Uno.");
			}
		}

		[Uno.NotImplemented]
		public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(
			"Content", typeof(object), 
			typeof(TreeViewNode), 
			new FrameworkPropertyMetadata(default(object)));

		[Uno.NotImplemented]
		public static DependencyProperty DepthProperty { get; } =
		DependencyProperty.Register(
			"Depth", typeof(int), 
			typeof(TreeViewNode), 
			new FrameworkPropertyMetadata(default(int)));

		[Uno.NotImplemented]
		public static DependencyProperty HasChildrenProperty { get; } =
		DependencyProperty.Register(
			"HasChildren", typeof(bool), 
			typeof(TreeViewNode), 
			new FrameworkPropertyMetadata(default(bool)));

		[Uno.NotImplemented]
		public static DependencyProperty IsExpandedProperty { get; } =
		DependencyProperty.Register(
			"IsExpanded", typeof(bool), 
			typeof(TreeViewNode), 
			new FrameworkPropertyMetadata(default(bool)));

		// Skipping already declared method Windows.UI.Xaml.Controls.TreeViewNode.TreeViewNode()
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.TreeViewNode()
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.Content.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.Content.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.Parent.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.IsExpanded.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.IsExpanded.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.HasChildren.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.Depth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.HasUnrealizedChildren.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.HasUnrealizedChildren.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.Children.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.ContentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.DepthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.IsExpandedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewNode.HasChildrenProperty.get
	}
}
