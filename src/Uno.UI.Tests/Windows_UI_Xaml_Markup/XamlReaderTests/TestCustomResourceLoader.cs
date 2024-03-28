using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Resources;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	internal class TestCustomXamlResourceLoader : CustomXamlResourceLoader
	{
		internal Dictionary<string, object> TestCustomResources { get; set; } = new Dictionary<string, object>();

		public string LastResourceId { get; set; }
		public string LastObjectType { get; set; }
		public string LastPropertyName { get; set; }
		public string LastPropertyType { get; set; }

		protected override object GetResource(string resourceId, string objectType, string propertyName, string propertyType)
		{
			LastResourceId = resourceId;
			LastObjectType = objectType;
			LastPropertyName = propertyName;
			LastPropertyType = propertyType;
			if (TestCustomResources.ContainsKey(resourceId) && TestCustomResources[resourceId].GetType().FullName == propertyType)
			{
				return TestCustomResources[resourceId];
			}
			return null;
		}
	}
}
