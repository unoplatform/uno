extern alias __ms;
extern alias __uno;

namespace Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class XamlXmlReaderSettings
	{
		public XamlXmlReaderSettings()
		{
			if (XamlConfig.IsUnoXaml)
			{
				UnoInner = new __uno::Uno.Xaml.XamlXmlReaderSettings();
			}
			else
			{
				MsInner = new __ms::System.Xaml.XamlXmlReaderSettings();
			}
		}

		public bool ProvideLineInfo
		{
			get => XamlConfig.IsUnoXaml ? UnoInner.ProvideLineInfo : MsInner.ProvideLineInfo;
			set
			{
				if (XamlConfig.IsUnoXaml)
				{
					UnoInner.ProvideLineInfo = value;
				}
				else
				{
					MsInner.ProvideLineInfo = value;
				}
			}
		}

		public __uno::Uno.Xaml.XamlXmlReaderSettings UnoInner { get; internal set; }
		public __ms::System.Xaml.XamlXmlReaderSettings MsInner { get; internal set; }
	}
}