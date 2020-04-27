using SystemXmlImplementation = System.Xml.XmlImplementation;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlDomImplementation
	{
		private readonly XmlDocument _owner;
		internal readonly SystemXmlImplementation _backingImplementation;

		internal XmlDomImplementation(XmlDocument owner, SystemXmlImplementation backingImplementation)
		{
			_owner = owner;
			_backingImplementation = backingImplementation;
		}

		public bool HasFeature(string feature, object version) => _backingImplementation.HasFeature(feature, version?.ToString());
	}
}
