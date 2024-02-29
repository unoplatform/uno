namespace Windows.Data.Xml.Dom
{
	public partial interface IXmlNodeSelector
	{
		IXmlNode? SelectSingleNode(string xpath);

		XmlNodeList? SelectNodes(string xpath);
	}
}
