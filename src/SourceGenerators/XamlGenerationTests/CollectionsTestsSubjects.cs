using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace XamlGenerationTests
{
	[ContentProperty(Name = nameof(MyCollection))]
	public partial class CollectionsTest_Control : Control
	{
		public CollectionsTest_Collection MyCollection { get; set; }
	}

	public partial class CollectionsTest_Collection : List<CollectionsTest_Item>
	{
		public int MyProperty { get; set; }
	}

	public partial class CollectionsTest_Item
	{

	}
}
