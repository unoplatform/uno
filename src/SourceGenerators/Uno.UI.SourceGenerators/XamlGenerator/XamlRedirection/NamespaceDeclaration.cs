extern alias __uno;

namespace Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class NamespaceDeclaration
	{
		private __uno::Uno.Xaml.NamespaceDeclaration _unoNs;

		public NamespaceDeclaration(__uno::Uno.Xaml.NamespaceDeclaration ns)
			=> _unoNs = ns;

		public string Namespace => _unoNs.Namespace;
		public string Prefix => _unoNs.Prefix;
	}
}
