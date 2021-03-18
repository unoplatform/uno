using System;
using Uno;

namespace Windows.Globalization.NumberFormatting
{
	public  partial class SignificantDigitsNumberRounder : INumberRounder
	{
		public uint SignificantDigits { get; set; } = 0;

		[NotImplemented]
		public RoundingAlgorithm RoundingAlgorithm { get; set; } = RoundingAlgorithm.RoundHalfUp;

		public SignificantDigitsNumberRounder() 
		{
		}

		[NotImplemented]
		public int RoundInt32( int value)
		{
			throw new global::System.NotImplementedException("The member int SignificantDigitsNumberRounder.RoundInt32(int value) is not implemented in Uno.");
		}

		[NotImplemented]
		public uint RoundUInt32( uint value)
		{
			throw new global::System.NotImplementedException("The member uint SignificantDigitsNumberRounder.RoundUInt32(uint value) is not implemented in Uno.");
		}

		[NotImplemented]
		public long RoundInt64( long value)
		{
			throw new global::System.NotImplementedException("The member long SignificantDigitsNumberRounder.RoundInt64(long value) is not implemented in Uno.");
		}

		[NotImplemented]
		public  ulong RoundUInt64( ulong value)
		{
			throw new global::System.NotImplementedException("The member ulong SignificantDigitsNumberRounder.RoundUInt64(ulong value) is not implemented in Uno.");
		}

		public  float RoundSingle( float value)
		{
			return (float)Math.Round(value, (int)SignificantDigits, MidpointRounding.AwayFromZero);
		}

		public  double RoundDouble( double value)
		{
			return Math.Round(value, (int)SignificantDigits, MidpointRounding.AwayFromZero);
		}
	}
}
