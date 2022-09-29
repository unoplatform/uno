#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uno.UI.DataBinding
{
	public interface IBindingAdapter
	{
		void SetValue(object instance, object value);
		object GetValue(object instance);

		Type TargetType { get; }
	}
}
