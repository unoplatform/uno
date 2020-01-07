using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Xaml
{
	public static class BindingHelper
	{
		public static Binding SetBindingXBindProvider(Binding binding, object compiledSource, Func<object, object> xBindSelector, string[] propertyPaths = null)
		{
			binding.SetBindingXBindProvider(compiledSource, xBindSelector, propertyPaths);

			return binding;
		}
	}
}
