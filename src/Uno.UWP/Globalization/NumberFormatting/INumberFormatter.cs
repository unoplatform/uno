namespace Windows.Globalization.NumberFormatting
{
	public partial interface INumberFormatter
	{
		string Format(long value);
		string Format(ulong value);
		string Format(double value);
	}
}
