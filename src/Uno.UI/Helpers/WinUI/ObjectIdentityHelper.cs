#nullable enable

using System.Runtime.CompilerServices;
using System.Threading;

namespace Uno.UI.Helpers.WinUI;

/// <summary>
/// Stands in for the pointer-address identities WinUI formats with "%p" / "0x%zx":
/// a stable, non-zero id per live object, compared by reference.
/// </summary>
internal static class ObjectIdentityHelper
{
	private static readonly ConditionalWeakTable<object, IdHolder> s_ids = new();
	private static long s_lastId;

	public static ulong GetId(object o) => s_ids.GetValue(o, static _ => new IdHolder((ulong)Interlocked.Increment(ref s_lastId))).Id;

	private sealed class IdHolder(ulong id)
	{
		public ulong Id { get; } = id;
	}
}
