#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.DataBinding
{
	public interface IBindingItem
	{
		string PropertyName { get; }
		Type PropertyType { get; }
		object DataContext { get; }
	}
}
