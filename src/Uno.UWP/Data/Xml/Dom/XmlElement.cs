using SystemXmlElement = System.Xml.XmlElement;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlElement : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly XmlDocument _owner;
		private readonly SystemXmlElement _backingElement;

		internal XmlElement(XmlDocument owner, SystemXmlElement backingElement)
		{
			_owner = owner;
			_backingElement = backingElement;
		}

		public void SetAttribute(string attributeName, string attributeValue) =>
			_backingElement.SetAttribute(attributeName, attributeValue);
	}
}
