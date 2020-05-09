namespace Windows.Data.Xml.Dom
{
	public  partial interface IXmlText : IXmlCharacterData, IXmlNode, IXmlNodeSelector, IXmlNodeSerializer
	{
		IXmlText SplitText( uint offset);
	}
}
