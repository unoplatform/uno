using System;
using System.Linq;
using Microsoft.UI.Xaml.Markup.Reader;

namespace Microsoft.UI.Xaml.Markup
{
	public partial class XamlReader
	{
		internal XamlReader()
		{
		}

		public static object Load(string xaml)
		{
			var r = new XamlStringParser();

			var builder = new XamlObjectBuilder(r.Parse(xaml));
			var result = builder.Build();

			// Like WinUI, the loaded content is a namescope of its own, so its names don't reach the tree it joins.
			if (result is FrameworkElement root && NameScope.GetNameScope(root) is null)
			{
				var nameScope = new NameScope { Owner = root };
				if (!string.IsNullOrEmpty(root.Name))
				{
					nameScope.RegisterName(root.Name, root);
				}

				NameScope.SetNameScope(root, nameScope);
			}

			return result;
		}

		public static object LoadWithInitialTemplateValidation(string xaml)
		{
			return Load(xaml);
		}

		internal static object LoadUsingXClass(string xaml, string fileUri)
		{
			var r = new XamlStringParser();

			var builder = new XamlObjectBuilder(r.Parse(xaml), fileUri);

			return builder.Build(createInstanceFromXClass: true);
		}

		internal static void LoadUsingComponent(string xaml, object component, string fileUri = null)
		{
			var r = new XamlStringParser();

			var builder = new XamlObjectBuilder(r.Parse(xaml), fileUri);

			builder.Build(component);
		}
	}
}
