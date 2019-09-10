using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Uno.UI
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static class ResourceResolver
	{
		/// <summary>
		/// The master system resources dictionary.
		/// </summary>
		private static ResourceDictionary MasterDictionary => Uno.UI.GlobalStaticResources.MasterDictionary;

		/// <summary>
		/// Performs a one-time, typed resolution of a named resource, using Application.Resources.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="context">Currently unused. In place to have the possibility to modify the lookup mechanism without introducing a binary breaking change.</param>
		/// <returns></returns>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static T ResolveResourceStatic<T>(object key, object context = null)
		{
			if (TryTopLevelRetrieval(key, out var value) && value is T tValue)
			{
				return tValue;
			}

			return default(T);
		}

		/// <summary>
		/// Apply a StaticResource or ThemeResource assignment to a DependencyProperty of a DependencyObject. The assignment will be provisionally
		/// made immediately using Application.Resources if possible, and retried at load-time using the visual-tree scope.
		/// </summary>
		/// <param name="owner">Owner of the property</param>
		/// <param name="property">The property to assign</param>
		/// <param name="resourceKey">Key to the resource</param>
		/// <param name="context">Currently unused. In place to have the possibility to modify the lookup mechanism without introducing a binary breaking change.</param>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void ApplyResource(DependencyObject owner, DependencyProperty property, object resourceKey, bool isThemeResourceExtension, object context = null)
		{
			// Set initial value based on statically-available top-level resources.
			if (TryTopLevelRetrieval(resourceKey, out var value))
			{
				owner.SetValue(property, value);
			}

			(owner as IDependencyObjectStoreProvider).Store.SetBinding(property, new ResourceBinding(resourceKey, isThemeResourceExtension));
		}

		/// <summary>
		/// Tries to retrieve a resource from top-level resources (Application-level and system level).
		/// </summary>
		/// <param name="resourceKey">The resource key</param>
		/// <param name="value">Out parameter to which the retrieved resource is assigned.</param>
		/// <returns>True if the resource was found, false if not.</returns>
		private static bool TryTopLevelRetrieval(object resourceKey, out object value)
		{
			return Application.Current.Resources.TryGetValue(resourceKey, out value) ||
				MasterDictionary.TryGetValue(resourceKey, out value);
		}
	}
}
