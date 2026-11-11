namespace UnoApp50;

// Uses syntax of the SDK's default C# version: hot reload of this project fails if the
// dev-server compiler is older (spec 061 G2).
internal static class CSharpLatestSyntax
{
	// Labeled break and continue: C# 15.
	public static int CountUntilFirstSkip()
	{
		var count = 0;
		outer: for (var i = 0; i < 3; i++)
		{
			for (var j = 0; j < 3; j++)
			{
				if (j == 1)
				{
					continue outer;
				}

				count++;
			}
		}

		return count;
	}
}
