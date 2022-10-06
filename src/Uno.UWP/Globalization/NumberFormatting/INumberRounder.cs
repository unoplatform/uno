namespace Windows.Globalization.NumberFormatting
{
	public partial interface INumberRounder
	{
		int RoundInt32(int value);
		uint RoundUInt32(uint value);
		long RoundInt64(long value);
		ulong RoundUInt64(ulong value);
		float RoundSingle(float value);
		double RoundDouble(double value);
	}
}
