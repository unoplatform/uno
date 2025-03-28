using System;
using Uno.UI.Xaml;
using Windows.UI.Xaml;

namespace Uno.UI
{
	/// <summary>
	/// Metadata update handler used to reset caches when changes are applied
	/// by the hot reload engine.
	/// </summary>
	internal class RuntimeTypeMetadataUpdateHandler
	{
		public static void ClearCache(Type[] types)
		{
			DataBinding.BindingPropertyHelper.ClearCaches();
		}
	}
}
