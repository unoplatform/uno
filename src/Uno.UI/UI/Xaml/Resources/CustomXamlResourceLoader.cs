using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Resources
{
	public partial class CustomXamlResourceLoader
	{
		public static CustomXamlResourceLoader Current { get; set; }

		public CustomXamlResourceLoader() { }

		protected virtual object GetResource(string resourceId, string objectType, string propertyName, string propertyType)
		{
			throw new NotImplementedException(); // This method must be implemented by deriving classes.
		}

		internal object GetResourceInternal(string resourceId, string objectType, string propertyName, string propertyType)
			=> GetResource(resourceId, objectType, propertyName, propertyType);
	}
}
