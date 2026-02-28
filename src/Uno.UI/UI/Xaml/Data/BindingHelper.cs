#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

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
		public static Binding SetBindingXBindProvider(Binding binding, object compiledSource, Func<object, (bool, object)> xBindSelector, Action<object, object>? xBindBack, Type? sourceType, string[]? propertyPaths = null)
		{
			binding.SetBindingXBindProvider(compiledSource, xBindSelector, xBindBack, sourceType, propertyPaths);
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SuspendXBind(this DependencyObject instance)
		{
			// DependencyObjectStore, DependencyPropertyDetailsCollection, and BindingExpression
			// all keeps track of the binding suspension state. Since we only care about x:Bind here,
			// it would be easier to skip straight to the BindingExpression, ignoring the first two.
			(instance as IDependencyObjectStoreProvider)?.Store.SuspendCompiledBindings();
		}


		public static void UpdateResourceBindings(this DependencyObject instance) => UpdateResourceBindings(instance, resourceContextProvider: null);
		public static void UpdateResourceBindings(this DependencyObject instance, FrameworkElement? resourceContextProvider)
		{
			if (instance is IDependencyObjectStoreProvider provider)
			{
				provider.Store.ApplyElementNameBindings();
				provider.Store.UpdateResourceBindings(ResourceUpdateReason.ResolvedOnLoading, resourceContextProvider);
			}
		}
	}
}
