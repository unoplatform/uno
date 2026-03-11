#if DEBUG
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Uno.UI;
using Uno.Extensions;
using System.ComponentModel;
using Uno.UI.Xaml;
using System.Linq;
using System.Diagnostics;
using Windows.UI.Input.Spatial;

using ResourceKey = Microsoft.UI.Xaml.SpecializedResourceDictionary.ResourceKey;
using System.Runtime.CompilerServices;

namespace Microsoft.UI.Xaml
{
	partial class ResourceDictionary
	{
#if DEBUG && DEBUG_SET_RESOURCE_SOURCE
		private void TryApplySource(object value, in ResourceKey resourceKey)
		{
			if (value is DependencyObject dependencyObject && GetResourceSource(dependencyObject) == null)
			{
				SetResourceSource(dependencyObject, new DebugResourceSource(resourceKey, this));
			}
		}
#endif

		/// <summary>
		/// Get the theme associated with this dictionary, if it's part of <see cref="ThemeDictionaries"/> of another dictionary
		/// </summary>
		public string GetThemeKey()
		{
			var source = GetResourceSource(this);

			return source?.ResourceKey.Key ?? "";
		}

		public ResourceDictionary? GetContainingResourceDictionary()
		{
			var source = GetResourceSource(this);

			return source?.ContainingDictionary;
		}


		internal static DebugResourceSource GetResourceSource(DependencyObject obj)
		{
			return (DebugResourceSource)obj.GetValue(ResourceSourceProperty);
		}

		internal static void SetResourceSource(DependencyObject obj, DebugResourceSource value)
		{
			obj.SetValue(ResourceSourceProperty, value);
		}

		// Using a DependencyProperty as the backing store for ResourceSource.  This enables animation, styling, binding, etc...
		[DynamicDependency(nameof(GetResourceSource))]
		[DynamicDependency(nameof(SetResourceSource))]
		internal static readonly DependencyProperty ResourceSourceProperty =
			DependencyProperty.RegisterAttached("ResourceSource", typeof(DebugResourceSource), typeof(ResourceDictionary), new FrameworkPropertyMetadata(null));


		internal class DebugResourceSource
		{
			public ResourceKey ResourceKey { get; }

			private readonly WeakReference<ResourceDictionary> _containingDictionaryRef;
			public ResourceDictionary? ContainingDictionary => _containingDictionaryRef.GetTarget();

			public DebugResourceSource(ResourceKey resourceKey, ResourceDictionary containingDictionary)
			{
				ResourceKey = resourceKey;
				_containingDictionaryRef = new WeakReference<ResourceDictionary>(containingDictionary);
			}

		}


	}
}
#endif
