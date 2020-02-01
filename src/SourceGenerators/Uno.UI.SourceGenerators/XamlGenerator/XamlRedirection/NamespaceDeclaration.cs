extern alias __ms;
extern alias __uno;

namespace Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class NamespaceDeclaration
	{
		private readonly __uno::Uno.Xaml.NamespaceDeclaration _unoNs;
		private readonly __ms::System.Xaml.NamespaceDeclaration _msNs;

		public NamespaceDeclaration(__ms::System.Xaml.NamespaceDeclaration ns)
			=> _msNs = ns;

		public NamespaceDeclaration(__uno::Uno.Xaml.NamespaceDeclaration ns)
			=> _unoNs = ns;

		public string Namespace => XamlConfig.IsUnoXaml ? _unoNs.Namespace : _msNs.Namespace;
		public string Prefix => XamlConfig.IsUnoXaml ? _unoNs.Prefix : _msNs.Prefix;
	}
}