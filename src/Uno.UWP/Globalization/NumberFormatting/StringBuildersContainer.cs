#nullable enable

using System;
using System.Text;
using System.Threading;

namespace Uno.Globalization.NumberFormatting;

internal class StringBuildersContainer : IDisposable
{
	private ThreadLocal<StringBuilder> _stringBuilder1Storage = new ThreadLocal<StringBuilder>(() => new StringBuilder());
	private ThreadLocal<StringBuilder> _stringBuilder2Storage = new ThreadLocal<StringBuilder>(() => new StringBuilder());

	public StringBuilder StringBuilder1 => _stringBuilder1Storage.Value!;
	public StringBuilder StringBuilder2 => _stringBuilder2Storage.Value!;

	public static StringBuildersContainer Instance { get; } = new StringBuildersContainer();

	public void Dispose()
	{
		_stringBuilder1Storage.Dispose();
		_stringBuilder2Storage.Dispose();
	}
}
