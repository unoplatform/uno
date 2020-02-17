namespace Microsoft.UI.Xaml.Controls
{
	internal struct MathToken
	{
		public MathToken(MathTokenType t, char c)
		{
			Type = t;
			Char = c;
			Value = double.NaN;
		}

		public MathToken(MathTokenType t, double d)
		{
			Type = t;
			Char = '\0';
			Value = d;
		}

		public MathTokenType Type { get; }

		public char Char { get; }

		public double Value { get; }
	}
}
