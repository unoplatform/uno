using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Uno.UI.RemoteControl.Server.Helpers
{
	public static class TelemetryHashHelper
	{
		public static string Hash(object? input)
		{
			if (input is null)
			{
				return "unknown";
			}

			var str = input switch
			{
				string s => s,
				char[] arr => new string(arr),
				_ => input.ToString() ?? string.Empty
			};

			if (str.Length == 0)
			{
				return "empty";
			}

			Span<byte> inputBytes = stackalloc byte[256];
			var byteCount = Encoding.UTF8.GetBytes(str, inputBytes);
			Span<byte> hashBytes = stackalloc byte[16];
			using (var md5 = MD5.Create())
			{
				md5.TryComputeHash(inputBytes.Slice(0, byteCount), hashBytes, out _);
			}

			Span<char> hex = stackalloc char[32];
			for (var i = 0; i < hashBytes.Length; i++)
			{
				int b = hashBytes[i];
				hex[i * 2] = GetHexChar(b >> 4);
				hex[i * 2 + 1] = GetHexChar(b & 0xF);
			}

			return new string(hex);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static char GetHexChar(int value) => (char)(value < 10 ? '0' + value : 'a' + (value - 10));
	}
}
