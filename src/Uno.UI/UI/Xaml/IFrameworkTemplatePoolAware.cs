using System;
using System.Collections.Generic;
using System.Text;
using Uno;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// An element that should be notified when the template in which it exists is being reused.
	/// </summary>
	[UnoOnly]
	internal interface IFrameworkTemplatePoolAware
	{
		/// <summary>
		/// A call in which to execute any logic that should take place when template is recycled.
		/// </summary>
		void OnTemplateRecycled();
	}
}
