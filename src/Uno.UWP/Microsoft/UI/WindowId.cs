using System;
using System.Diagnostics;
using System.Linq.Expressions;

// TODO: There is also WUX WindowId, but should be only in UWP - #13842
namespace Microsoft.UI;

[DebuggerDisplay("{Value}")]
#if HAS_UNO_WINUI
public
#else
internal
#endif
partial struct WindowId : IEquatable<WindowId>
{
	public ulong Value;

	public WindowId(ulong _Value)
		=> Value = _Value;

	public static bool operator ==(WindowId x, WindowId y)
		=> x.Value == y.Value;

	public static bool operator !=(WindowId x, WindowId y)
		=> x.Value != y.Value;

	public bool Equals(WindowId other)
		=> Value == other.Value;

	public override bool Equals(object obj)
		=> (obj is WindowId y) && Equals(y);

	public override int GetHashCode()
		=> Value.GetHashCode();
}
