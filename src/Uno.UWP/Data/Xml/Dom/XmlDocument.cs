using System.Collections.Generic;
using SystemXmlDocument = System.Xml.XmlDocument;
using SystemXmlElement = System.Xml.XmlElement;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlDocument : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private SystemXmlDocument _backingDocument = new SystemXmlDocument();
		private Dictionary<object, object> _systemXmlElementWrappings = new Dictionary<object, object>();

		public void LoadXml(string xml) => _backingDocument.LoadXml(xml);

		public IXmlNode SelectSingleNode(string xpath) => (IXmlNode)Wrap(_backingDocument.SelectSingleNode(xpath));

		internal object Wrap(object node)
		{
			if (!_systemXmlElementWrappings.TryGetValue(node, out var wrapped))
			{
				wrapped = CreateWrapper(node);
				_systemXmlElementWrappings.Add(node, wrapped);
			}
			return wrapped;
		}

		private object CreateWrapper(object node) =>
			node switch
			{
				SystemXmlElement element => new XmlElement(this, element),
				_ => throw new global::System.NotImplementedException(),
			};
	}
}
