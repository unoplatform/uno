using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace MUXControlsTestApp.Utilities
{
	public partial class MUXTestPage : Page
	{
		public static DependencyObject SearchVisualTree(DependencyObject root, string name)
		{
			int size = VisualTreeHelper.GetChildrenCount(root);
			DependencyObject child = null;

			for (int i = 0; i < size && child == null; i++)
			{
				DependencyObject depObj = VisualTreeHelper.GetChild(root, i);
				FrameworkElement fe = depObj as FrameworkElement;

				if (fe.Name.Equals(name))
				{
					child = fe;
				}
				else
				{
					child = SearchVisualTree(fe, name);
				}
			}

			return child;
		}

		public static DependencyObject FindVisualChildByName(FrameworkElement parent, string name)
		{
			if (parent.Name == name)
			{
				return parent;
			}

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

			for (int i = 0; i < childrenCount; i++)
			{
				FrameworkElement childAsFE = VisualTreeHelper.GetChild(parent, i) as FrameworkElement;

				if (childAsFE != null)
				{
					DependencyObject result = FindVisualChildByName(childAsFE, name);

					if (result != null)
					{
						return result;
					}
				}
			}

			return null;
		}

		public static List<T> FindVisualChildrenByType<T>(FrameworkElement parent) where T : class
		{
			List<T> children = new List<T>();
			T parentAsT = parent as T;

			if (parentAsT != null)
			{
				children.Add(parentAsT);
			}

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

			for (int i = 0; i < childrenCount; i++)
			{
				FrameworkElement childAsFE = VisualTreeHelper.GetChild(parent, i) as FrameworkElement;

				if (childAsFE != null)
				{
					List<T> result = FindVisualChildrenByType<T>(childAsFE);
					children.AddRange(result);
				}
			}

			return children;
		}
	}
}
