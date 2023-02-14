using System;
using System.Linq;
using Microsoft.UI.Xaml.Markup.Reader;

namespace Microsoft.UI.Xaml.Markup
{
	public partial class XamlReader
	{
		public static object Load(string xaml)
		{
			var r = new XamlStringParser();

			var builder = new XamlObjectBuilder(r.Parse(xaml));

			return builder.Build();
		}

		public static object LoadWithInitialTemplateValidation(string xaml)
		{
			return Load(xaml);
		}
		internal static object LoadUsingXClass(string xaml)
		{
			var r = new XamlStringParser();

			var builder = new XamlObjectBuilder(r.Parse(xaml));

			return builder.Build(createInstanceFromXClass: true);
		}

		internal static void LoadUsingComponent(string xaml, object component)
		{
			var r = new XamlStringParser();

			var builder = new XamlObjectBuilder(r.Parse(xaml));

			builder.Build(component);
		}
	}
}
