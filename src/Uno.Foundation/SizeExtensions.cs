namespace Windows.Foundation;

internal static class SizeExtensions
{
	internal static double AspectRatio(this Size size)
	{
		var w = size.Width;
		var h = size.Height;
		switch (w)
		{
			case double.NegativeInfinity:
				return -1;
			case double.PositiveInfinity:
				return 1;
			case double.NaN:
				return 1;
			case 0.0d:
				return 1;
		}
		switch (h)
		{
			case double.NegativeInfinity:
				return -1;
			case double.PositiveInfinity:
				return 1;
			case double.NaN:
				return 1;
			case 0.0d:
				return 1; // special case
			case 1.0d:
				return w;
		}
		return w / h;
	}

#if __IOS__ || __MACOS__
	internal static double AspectRatio(this CoreGraphics.CGSize size)
	{
		var w = size.Width;
		var h = size.Height;
		if (w == nfloat.NegativeInfinity)
		{
			return -1;
		}
		else if (w == nfloat.PositiveInfinity)
		{
			return 1;
		}
		else if (w == nfloat.NaN)
		{
			return 1;
		}
		else if (w == 0.0d)
		{
			return 1;
		}
		if (h == nfloat.NegativeInfinity)
		{
			return -1;
		}
		else if (h == nfloat.PositiveInfinity)
		{
			return 1;
		}
		else if (h == nfloat.NaN)
		{
			return 1;
		}
		else if (h == 0.0d)
		{
			return 1; // special case
		}
		else if (h == 1.0d)
		{
			return w;
		}
		return w / h;
	}
#endif
}
