extern alias __uno;

namespace Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class XamlSchemaContext
	{
		public XamlSchemaContext()
		{
			UnoInner = new __uno::Uno.Xaml.XamlSchemaContext();
		}

		public XamlSchemaContext(System.Collections.Generic.IEnumerable<System.Reflection.Assembly> enumerable)
		{
			UnoInner = new __uno::Uno.Xaml.XamlSchemaContext(enumerable);
		}

		public __uno::Uno.Xaml.XamlSchemaContext UnoInner { get; }
	}
}
