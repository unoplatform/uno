#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Xaml
{
	public static class BindingHelper
	{
		public static Binding SetBindingXBindProvider(Binding binding, object compiledSource, Func<object, object> xBindSelector, string[]? propertyPaths = null)
			=> SetBindingXBindProvider(binding, compiledSource, xBindSelector, null, propertyPaths);

		public static Binding SetBindingXBindProvider(Binding binding, object compiledSource, Func<object, object> xBindSelector, Action<object, object>? xBindBack, string[]? propertyPaths = null)
		{
			binding.SetBindingXBindProvider(compiledSource, xBindSelector, xBindBack, propertyPaths);
			return binding;
		}

		public static AttachedDependencyObject GetDependencyObjectForXBind(this object instance)
			=> DependencyObjectExtensions.GetAttachedDependencyObject(instance);

		public static void ApplyXBind(this DependencyObject instance)
			=> (instance as IDependencyObjectStoreProvider)?.Store.ApplyCompiledBindings();

		public static void UpdateResourceBindings(this DependencyObject instance)
		{
			if(instance is IDependencyObjectStoreProvider provider)
			{
				provider.Store.ApplyElementNameBindings();
				provider.Store.UpdateResourceBindings(false);
			}
		}
	}
}
