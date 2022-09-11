namespace Windows.Globalization.NumberFormatting
{
	public partial interface INumberParser
	{
		long? ParseInt(string text);
		ulong? ParseUInt(string text);
		double? ParseDouble(string text);
	}
}
