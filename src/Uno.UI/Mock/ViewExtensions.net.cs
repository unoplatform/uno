using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI
{
	public static partial class ViewExtensions
	{
		/// <summary>
		/// Gets an enumerator containing all the children of a View group
		/// </summary>
		/// <param name="group"></param>
		/// <returns></returns>
		public static IEnumerable<object> GetChildren(this object group) => (IEnumerable<object>)(group as FrameworkElement)?._children ?? new object[0];

		public static FrameworkElement GetTopLevelParent(this UIElement view) => throw new NotImplementedException();
	}
}
