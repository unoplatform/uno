extern alias __ms;
extern alias __uno;

namespace Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class XamlSchemaContext
	{
		public XamlSchemaContext()
		{
			if (XamlConfig.IsUnoXaml)
			{
				UnoInner = new __uno::Uno.Xaml.XamlSchemaContext();
			}
			else
			{
				MsInner = new __ms::System.Xaml.XamlSchemaContext();
			}
		}

		public XamlSchemaContext(System.Collections.Generic.IEnumerable<System.Reflection.Assembly> enumerable)
		{
			if (XamlConfig.IsUnoXaml)
			{
				UnoInner = new __uno::Uno.Xaml.XamlSchemaContext(enumerable);
			}
			else
			{
				MsInner = new __ms::System.Xaml.XamlSchemaContext(enumerable);
			}
		}

		public __ms::System.Xaml.XamlSchemaContext MsInner { get; }

		public __uno::Uno.Xaml.XamlSchemaContext UnoInner { get; }
	}
}