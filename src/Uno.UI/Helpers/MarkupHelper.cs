using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI.Helpers
{
	/// <summary>
	/// A set of Uno specific markup helpers
	/// </summary>
	public static class MarkupHelper
	{
		/// <summary>
		/// Sets the x:Uid member on a element implementing <see cref="IXUidProvider"/>
		/// </summary>
		/// <param name="target">The target object</param>
		/// <param name="uid">The new uid to set</param>
		public static void SetXUid(object target, string uid)
		{
			if(target is IXUidProvider provider)
			{
				provider.Uid = uid;
			}
		}

		/// <summary>
		/// Gets the Uid defined via <see cref="SetXUid(object, string)"/>
		/// </summary>
		/// <param name="target">The target object</param>
		/// <returns>A the x:Uid value</returns>
		public static string GetXUid(object target)
			=> target is IXUidProvider provider ? provider.Uid : "";

	}
}
