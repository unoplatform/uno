using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI
{
	internal static class ToolkitHelper
	{
		public static DependencyProperty GetProperty(string ownerTypeName, string propertyName)
		{
			var ownerType = Type.GetType(ownerTypeName + ",Uno.UI.Toolkit");
			var dp = DependencyProperty.GetProperty(ownerType, propertyName);
			return dp;
		}
	}
}
