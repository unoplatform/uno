using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml
{
	public partial class Application
	{
		private Dictionary<string, string> _loadableComponents;

		internal bool IsLoadableComponent(Uri resource)
		{
			EnsureLoadableComponents();

			return _loadableComponents.ContainsKey(resource.OriginalString);
		}

		public static void LoadComponent(object component, Uri resourceLocator)
		{
			if(Current._loadableComponents.TryGetValue(resourceLocator.OriginalString, out var document))
			{
				XamlReader.LoadUsingComponent(document, component);
			}
		}

		internal static void RegisterComponent(Uri resourceLocator, string xaml)
		{
			Current._loadableComponents[resourceLocator.OriginalString] = xaml;
		}

		private void EnsureLoadableComponents() => _loadableComponents ??= new Dictionary<string, string>();
	}
}
