using System;
using System.Linq;

namespace Uno.UI.Benchmarks.Publisher;

internal static class ArgParser
{
	public static string Required(string[] args, string flag)
		=> Optional(args, flag) ?? throw new ArgumentException($"Required flag {flag} missing");

	public static string? Optional(string[] args, string flag)
	{
		var idx = Array.IndexOf(args, flag);
		if (idx < 0 || idx + 1 >= args.Length)
		{
			return null;
		}
		return args[idx + 1];
	}
}
