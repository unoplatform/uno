using System;
using System.Linq;
using Windows.UI.Xaml.Markup.Reader;

namespace Windows.UI.Xaml.Markup
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
    }
}
