using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace UITests.Shared.Windows_UI_Xaml.MarkupExtension
{
	[MarkupExtensionReturnType(ReturnType = typeof(EntityObject))]
	public class Entity : Windows.UI.Xaml.Markup.MarkupExtension
	{
		public string TextValue { get; set; }

		public int IntValue { get; set; }

		protected override object ProvideValue()
		{
			return new EntityObject()
			{
				StringProperty = TextValue,
				IntProperty = IntValue
			};
		}
	}

	public class EntityObject
	{
		public string StringProperty { get; set; } = string.Empty;

		public int IntProperty { get; set; }
	}
}
