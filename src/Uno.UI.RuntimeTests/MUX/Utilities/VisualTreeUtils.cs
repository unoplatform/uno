// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace MUXControlsTestApp.Utilities
{
    public static class VisualTreeUtils
    {
        public static T FindElementOfTypeInSubtree<T>(this DependencyObject element)
#if !NETFX_CORE
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
    }
}
