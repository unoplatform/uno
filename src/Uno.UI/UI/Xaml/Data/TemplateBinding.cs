using Uno.UI.DataBinding;
using System;
using System.Collections.Generic;
using System.Text;

#if XAMARIN
using IValueConverter = Windows.UI.Xaml.Data.IValueConverter;
#else
#endif

namespace Windows.UI.Xaml.Data
{
	/// <summary>
	/// A template binding definition, using a TemplatedParent relative source
	/// </summary>
	public class TemplateBinding : Binding
	{
		public TemplateBinding(PropertyPath path = default(PropertyPath), IValueConverter converter = null, object converterParameter = null)
			: base(path, converter, converterParameter)
		{
			RelativeSource = RelativeSource.TemplatedParent;
		}
	}
}
