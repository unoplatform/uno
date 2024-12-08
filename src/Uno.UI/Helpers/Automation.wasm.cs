using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
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
		[JSExport]
		internal static string GetDependencyPropertyValue(int handle, string propertyName)
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

		/// <summary>
		/// Sets the specified dependency property value using the format "name|value"
		/// </summary>
		/// <param name="dependencyPropertyNameAndValue">The name and value of the property</param>
		/// <param name="handle">The GCHandle of the UIElement to use</param>
		/// <returns>The currenty set value at the Local precedence</returns>
		[Preserve]
		[JSExport]
		internal static string SetDependencyPropertyValue(int handle, string dependencyPropertyNameAndValue)
		{
			// Dispatch to right object, if we can find it
			if (UIElement.GetElementFromHandle(handle) is UIElement element)
			{
				return UIElement.SetDependencyPropertyValueInternal(element, dependencyPropertyNameAndValue);
			}
			else
			{
				Console.Error.WriteLine($"No UIElement found for htmlId \"{handle}\"");
				return "";
			}
		}
	}
}
