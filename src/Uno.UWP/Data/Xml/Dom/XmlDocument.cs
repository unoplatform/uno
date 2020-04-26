using System.Collections.Generic;
using SystemXmlDocument = System.Xml.XmlDocument;
using SystemXmlElement = System.Xml.XmlElement;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlDocument : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private SystemXmlDocument _backingDocument = new SystemXmlDocument();
		private Dictionary<object, object> _uwpToSystemXml = new Dictionary<object, object>();
		private Dictionary<object, object> _systemXmlToUwp = new Dictionary<object, object>();

		public void LoadXml(string xml) => _backingDocument.LoadXml(xml);

		public IXmlNode SelectSingleNode(string xpath) => (IXmlNode)Wrap(_backingDocument.SelectSingleNode(xpath));

		/// <summary>
		/// Wraps System.Xml node to UWP XML node.
		/// </summary>
		/// <param name="node">System.Xml node.</param>
		/// <returns>UWP XML node.</returns>
		internal object Wrap(object node)
		{
			if (!_systemXmlToUwp.TryGetValue(node, out var wrapped))
			{
				wrapped = CreateWrapper(node);
				_systemXmlToUwp.Add(node, wrapped);
			}
			return wrapped;
		}

		/// <summary>
		/// Wraps UWP XML node to System.Xml node.
		/// </summary>
		/// <param name="node">UWP XML node.</param>
		/// <returns>System.Xml node.</returns>
		internal object Unwrap(object node)
		{
			if (!_uwpToSystemXml.TryGetValue(node, out var wrapped))
			{
				wrapped = CreateWrapper(node);
				_uwpToSystemXml.Add(node, wrapped);
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
