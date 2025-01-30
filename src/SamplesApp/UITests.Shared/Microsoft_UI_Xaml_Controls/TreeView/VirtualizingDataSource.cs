using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.TreeViewTests
{
	public class CustomContent
	{
		public List<CustomContent> Children = new List<CustomContent>();

		private readonly int nestingLevel;
		private readonly int index;

		public CustomContent(int nestingLevel = 3, int index = 0)
		{
			this.nestingLevel = nestingLevel;
			this.index = index;
			if (nestingLevel <= 0)
			{
				return;
			}
			for (int i = 0; i < 4; i++)
			{
				Children.Add(new CustomContent(nestingLevel - 1, i));
			}
		}

		public override string ToString()
		{
			return $"Item: {this.index}; layer: {this.nestingLevel}";
		}

		public TreeViewNode GetTreeViewNode()
		{
			var node = new TreeViewNode();
			node.Content = this;
			node.HasUnrealizedChildren = this.Children.Count != 0;
			return node;
		}
	}

	public class VirtualizingDataSource
	{
		public static void FillTreeNode(TreeViewNode node)
		{
			var customContent = (CustomContent)node.Content;
			if (customContent != null)
			{
				if (node.HasUnrealizedChildren)
				{
					foreach (var child in customContent.Children)
					{
						node.Children.Add(child.GetTreeViewNode());
					}
				}
				node.HasUnrealizedChildren = false;
			}
		}

		public static void EmptyTreeNode(TreeViewNode node)
		{
			node.Children.Clear();
			node.HasUnrealizedChildren = true;
		}
	}
}
