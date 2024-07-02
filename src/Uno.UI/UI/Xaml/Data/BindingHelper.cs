#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Xaml
{
	public static class BindingHelper
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Binding SetBindingXBindProvider(Binding binding, object compiledSource, Func<object, object> xBindSelector, string[]? propertyPaths = null)
			=> SetBindingXBindProvider(binding, compiledSource, xBindSelector, null, propertyPaths);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Binding SetBindingXBindProvider(Binding binding, object compiledSource, Func<object, object> xBindSelector, Action<object, object>? xBindBack, string[]? propertyPaths = null)
		{
			binding.SetBindingXBindProvider(compiledSource, o => (true, xBindSelector(o)), xBindBack, propertyPaths);
			return binding;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Binding SetBindingXBindProvider(Binding binding, object compiledSource, Func<object, (bool, object)> xBindSelector, Action<object, object>? xBindBack, string[]? propertyPaths = null)
		{
			binding.SetBindingXBindProvider(compiledSource, xBindSelector, xBindBack, propertyPaths);
			return binding;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AttachedDependencyObject GetDependencyObjectForXBind(this object instance)
			=> DependencyObjectExtensions.GetAttachedDependencyObject(instance);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DependencyObject GetDependencyObjectForXBind(this DependencyObject instance)
			=> instance;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ApplyXBind(this DependencyObject instance)
			=> (instance as IDependencyObjectStoreProvider)?.Store.ApplyCompiledBindings();

		public static void UpdateResourceBindings(this DependencyObject instance)
		{
			if (instance is IDependencyObjectStoreProvider provider)
			{
				provider.Store.ApplyElementNameBindings();
				provider.Store.UpdateResourceBindings(ResourceUpdateReason.ResolvedOnLoading);
			}
		}
	}
}
