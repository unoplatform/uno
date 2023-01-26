using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	public class TestCodedResourceDictionary : ResourceDictionary
	{
		public TestCodedResourceDictionary()
		{
			this["c1"] = Colors.Red;
		}
	}
}
