namespace Windows.Data.Xml.Dom
{
	public  partial interface IXmlNodeSerializer 
	{
		string InnerText { get; set; }

		string GetXml();		
	}
}
