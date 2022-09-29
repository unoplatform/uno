#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace System.Windows
{
    [MarkupExtensionReturnType(typeof(object))]
    public class StaticResourceExtension : MarkupExtension
    {
        public StaticResourceExtension()
        {
        }

        public StaticResourceExtension(object resourceKey)
        {
        }


        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return null;
        }
    }
}
