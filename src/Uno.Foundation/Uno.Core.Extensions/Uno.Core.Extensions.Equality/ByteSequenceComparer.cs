using System.Collections.Immutable;
using System.Diagnostics;

namespace Uno.Equality;

internal static class ByteSequenceComparer
{
	// https://github.com/dotnet/roslyn/blob/6f47a0b611ab83ad1b7f8b404cc74098d37e283d/src/Compilers/Core/Portable/Collections/ByteSequenceComparer.cs#L24-L45
	internal static bool Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
	{
		if (x == y)
		{
			return true;
		}

		if (x.IsDefault || y.IsDefault || x.Length != y.Length)
		{
			return false;
		}

		for (var i = 0; i < x.Length; i++)
		{
			if (x[i] != y[i])
			{
				return false;
			}
		}

		return true;
	}

	// https://github.com/dotnet/roslyn/blob/6f47a0b611ab83ad1b7f8b404cc74098d37e283d/src/Compilers/Core/Portable/Collections/ByteSequenceComparer.cs#L101-L105
	internal static int GetHashCode(ImmutableArray<byte> x)
	{
		Debug.Assert(!x.IsDefault);
		return Hash.GetFNVHashCode(x);
	}
}
