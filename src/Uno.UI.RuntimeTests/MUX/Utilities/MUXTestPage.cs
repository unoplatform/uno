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
