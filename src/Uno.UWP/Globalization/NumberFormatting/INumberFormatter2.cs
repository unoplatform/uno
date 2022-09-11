namespace Windows.Globalization.NumberFormatting
{
	public partial interface INumberFormatter2
	{
		string FormatInt(long value);
		string FormatUInt(ulong value);
		string FormatDouble(double value);
	}
}
