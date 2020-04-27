namespace Windows.Data.Xml.Dom
{
	public partial interface IXmlCharacterData : IXmlNode, IXmlNodeSelector, IXmlNodeSerializer
	{
		string Data { get; set; }

		uint Length { get; }

		string SubstringData(uint offset, uint count);

		void AppendData(string data);

		void InsertData(uint offset, string data);

		void DeleteData(uint offset, uint count);

		void ReplaceData(uint offset, uint count, string data);
	}
}
