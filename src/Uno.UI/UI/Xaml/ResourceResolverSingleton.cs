using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Resources;

namespace Uno.UI
{
	/// <summary>
	/// A wrapper for <see cref="ResourceResolver"/> methods following the singleton pattern.
	/// </summary>
	/// <remarks>
	/// The motivation is to avoid additional overhead associated with static method calls into types with static state. This is normally
	/// only called from Xaml-generated code.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public sealed class ResourceResolverSingleton
	{
		private static ResourceResolverSingleton _instance;
		public static ResourceResolverSingleton Instance
			=> _instance ??= new ResourceResolverSingleton();

		[EditorBrowsable(EditorBrowsableState.Never)]
		public object ResolveResourceStatic(object key, Type type, object context)
			=> ResourceResolver.ResolveResourceStatic(key, type, context);

		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ApplyResource(DependencyObject owner, DependencyProperty property, object resourceKey, bool isThemeResourceExtension, bool isHotReloadSupported, object context)
			=> ResourceResolver.ApplyResource(owner, property, resourceKey, isThemeResourceExtension, isHotReloadSupported, true, context);

		[EditorBrowsable(EditorBrowsableState.Never)]
		public object ResolveStaticResourceAlias(string resourceKey, object parseContext)
			=> ResourceResolver.ResolveStaticResourceAlias(resourceKey, parseContext);
	}
}
