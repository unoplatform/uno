using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace XamlGenerationTests
{
	public partial class TypeConvertersControl : FrameworkElement
	{
		public Type TypeProperty { get; set; }

		public Uri UriProperty { get; set; }
	}
}
