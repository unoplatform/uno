#nullable enable

using System;
using System.Text;
using System.Threading;

namespace Uno.Globalization.NumberFormatting;

internal class StringBuilderPool : IDisposable
{
	private ThreadLocal<StringBuilder> _stringBuilder1Storage = new ThreadLocal<StringBuilder>(() => new StringBuilder());
	private ThreadLocal<StringBuilder> _stringBuilder2Storage = new ThreadLocal<StringBuilder>(() => new StringBuilder());

	public StringBuilder StringBuilder1 => _stringBuilder1Storage.Value!;
	public StringBuilder StringBuilder2 => _stringBuilder2Storage.Value!;

	public static StringBuilderPool Instance { get; } = new StringBuilderPool();

	public void Dispose()
	{
		_stringBuilder1Storage.Dispose();
		_stringBuilder2Storage.Dispose();
	}
}
