// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the Utils.h file from WinUI controls.
//

using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Helpers.WinUI
{
	internal class VisualStateUtil
	{
		public static VisualStateGroup GetVisualStateGroup(FrameworkElement control, string groupName)
		{
			VisualStateGroup group = null;
			var visualStateGroups = VisualStateManager.GetVisualStateGroups(control);
			if (visualStateGroups == null && control is ContentControl contentControl)
			{
				visualStateGroups = VisualStateManager.GetVisualStateGroups(contentControl);
			}

			if (visualStateGroups == null)
			{
				return group;
			}

			foreach (var visualStateGroup in visualStateGroups)
			{
				if (visualStateGroup.Name == groupName)
				{
					group = visualStateGroup;
					return group;
				}
			}
			return group;
		}

		public static bool VisualStateGroupExists(FrameworkElement control, string groupName)
		{
			return GetVisualStateGroup(control, groupName) != null;
		}

		public static void GoToStateIfGroupExists(Control control, string groupName, string stateName, bool useTransitions)
		{
			var visualStateGroup = GetVisualStateGroup(control, groupName);
			if (visualStateGroup != null)
			{
				VisualStateManager.GoToState(control, stateName, useTransitions);
			}
		}
	}

	internal class LayoutUtils
	{
		public static double MeasureAndGetDesiredWidthFor(UIElement element, Size availableSize)
		{
			double desiredWidth = 0;
			if (element != null)
			{
				element.Measure(availableSize);
				desiredWidth = element.DesiredSize.Width;
			}
			return desiredWidth;
		}

		public static double GetActualWidthFor(UIElement element)
		{
			return (element != null ? (element as FrameworkElement).ActualWidth : 0);
		}
	}

	class Util
	{
		public static Visibility VisibilityFromBool(bool visible)
		{
			return visible ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
