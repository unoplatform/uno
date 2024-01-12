#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Xml.Dom
{
	public partial interface IXmlNode : IXmlNodeSelector, IXmlNodeSerializer
	{
		XmlNamedNodeMap? Attributes { get; }

		XmlNodeList ChildNodes { get; }

		IXmlNode? FirstChild { get; }

		IXmlNode? LastChild { get; }

		object LocalName { get; }

		object NamespaceUri { get; }

		IXmlNode? NextSibling { get; }

		string NodeName { get; }

		NodeType NodeType { get; }

		object? NodeValue { get; set; }

		XmlDocument OwnerDocument { get; }

		IXmlNode? ParentNode { get; }

		object? Prefix { get; set; }

		IXmlNode? PreviousSibling { get; }

		bool HasChildNodes();

		IXmlNode? InsertBefore(IXmlNode newChild, IXmlNode referenceChild);

		IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild);

		IXmlNode RemoveChild(IXmlNode childNode);

		IXmlNode? AppendChild(IXmlNode newChild);

		IXmlNode CloneNode(bool deep);

		void Normalize();
	}
}
