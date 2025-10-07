using System;
using System.Collections.Generic;
using System.Text;

#if NETFX_CORE
using Windows.UI.Xaml.Controls;
#elif XAMARIN
#else
#endif

namespace Windows.UI.Xaml.Controls
{
	public static class StackPanelExtensions
	{
		/// <summary>
		/// Sets the orientation for this IStackPanel instance.
		/// </summary>
		/// <typeparam name="T">An IStackPanel type</typeparam>
		/// <param name="instance">An IStackPanel instance</param>
		/// <param name="orientation">The orientation</param>
		/// <returns>The IStackPanelInstance</returns>
		public static StackPanel Orientation(this StackPanel instance, Orientation orientation)
		{
#if NETFX_CORE || XAMARIN
			instance.Orientation = orientation;
#endif
			return instance;
		}
	}
}
