#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Breadcrumb_TestUI
{
	class TreeStructure
	{
		public TreeNode Root = null;

		public List<TreeNode> GetRootAsList()
		{
			return new List<TreeNode>() { Root };
		}

		public void UpdateParents()
		{
			Root.UpdateParents(null);
		}
	}

	public class TreeNode
	{
		private TreeNode Parent = null;
		public string Name { get; set; }

		public List<object> Children { get; set; } = new List<object>();

		public List<object> GetBreadCrumbPath()
		{
			List<object> path = new List<object>();

			TreeNode currentNode = this;
			while (currentNode != null)
			{
				path.Add(currentNode);
				currentNode = currentNode.Parent;
			}

			path.Reverse();

			return path;
		}

		public void UpdateParents(TreeNode parent)
		{
			Parent = parent;
			foreach (TreeNode node in Children)
			{
				node.UpdateParents(this);
			}
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
