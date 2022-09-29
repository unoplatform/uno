#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// A value changed listener for bindings.
	/// </summary>
	internal interface IValueChangedListener
	{
		void OnValueChanged(object value);
	}
}
