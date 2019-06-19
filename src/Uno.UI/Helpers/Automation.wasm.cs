using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI.Helpers
{
	/// <summary>
	/// A set of helper methods primarily used from UI tests
	/// </summary>
	public static partial class Automation
	{
		/// <summary>
		/// Gets a dependency property value
		/// </summary>
		[Preserve]
		public static string GetDependencyPropertyValue(int handle, string propertyName)
		{
			// Dispatch to right object, if we can find it
			if (UIElement.GetElementFromHandle(handle) is UIElement element)
			{
				return Convert.ToString(UIElement.GetDependencyPropertyValueInternal(element, propertyName), CultureInfo.InvariantCulture);
			}
			else
			{
				Console.Error.WriteLine($"No UIElement found for htmlId \"{handle}\"");
				return "";
			}
		}
	}
}
