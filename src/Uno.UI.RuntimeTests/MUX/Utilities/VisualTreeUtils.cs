// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace MUXControlsTestApp.Utilities
{
	public static class VisualTreeUtils
	{
		public static T FindVisualChildByType<T>(this DependencyObject element)
#if !WINAPPSDK
			where T : class, DependencyObject
#else
			where T : DependencyObject
#endif
		{
			if (element == null)
			{
				return null;
			}

			if (element is T elementAsT)
			{
				return elementAsT;
			}

			int childrenCount = VisualTreeHelper.GetChildrenCount(element);
			for (int i = 0; i < childrenCount; i++)
			{
				var result = VisualTreeHelper.GetChild(element, i).FindVisualChildByType<T>();
				if (result != null)
				{
					return result;
				}
			}

			return null;
		}

		public static T FindElementOfTypeInSubtree<T>(this DependencyObject element)
#if !WINAPPSDK
			where T : class, DependencyObject
#else
			where T : DependencyObject
#endif
		{
			if (element == null)
			{
				return null;
			}

			if (element is T)
			{
				return (T)element;
			}

			int childrenCount = VisualTreeHelper.GetChildrenCount(element);
			for (int i = 0; i < childrenCount; i++)
			{
				var result = FindElementOfTypeInSubtree<T>(VisualTreeHelper.GetChild(element, i));
				if (result != null)
				{
					return result;
				}
			}

			return null;
		}

		public static DependencyObject FindVisualChildByName(this FrameworkElement parent, string name)
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

		public static T FindVisualParentByType<T>(this DependencyObject element)
			where T :
#if HAS_UNO
			class,
#endif
			DependencyObject
		{
			if (element is null)
			{
				return null;
			}

			return element is T elementAsT
				? elementAsT
				: VisualTreeHelper.GetParent(element).FindVisualParentByType<T>();
		}

		public static List<T> FindVisualChildrenByType<T>(DependencyObject parent)
#if !WINAPPSDK
			where T : class, DependencyObject
#else
			where T : DependencyObject
#endif
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
				var childAsFE = VisualTreeHelper.GetChild(parent, i) as DependencyObject;

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
