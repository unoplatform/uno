extern alias __uno;

namespace Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class XamlXmlReaderSettings
	{
		public XamlXmlReaderSettings()
		{
			UnoInner = new __uno::Uno.Xaml.XamlXmlReaderSettings();
		}

		public bool ProvideLineInfo
		{
			get => UnoInner.ProvideLineInfo;
			set
			{
				UnoInner.ProvideLineInfo = value;
			}
		}

		public __uno::Uno.Xaml.XamlXmlReaderSettings UnoInner { get; internal set; }
	}
}
