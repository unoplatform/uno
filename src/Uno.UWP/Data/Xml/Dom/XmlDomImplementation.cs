using SystemXmlImplementation = System.Xml.XmlImplementation;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlDomImplementation
	{
		internal readonly SystemXmlImplementation _backingImplementation;

		internal XmlDomImplementation(SystemXmlImplementation backingImplementation)
		{
			_backingImplementation = backingImplementation;
		}

		public bool HasFeature(string feature, object version) => _backingImplementation.HasFeature(feature, version?.ToString()!);
	}
}
