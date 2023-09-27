using System;
using System.Diagnostics;
using System.Linq.Expressions;

//TODO:MZ: Waiting for https://github.com/unoplatform/uno/pull/13842
//#if !HAS_UNO_WINUI
//namespace Windows.UI;
//#else
namespace Microsoft.UI;

[DebuggerDisplay("{Value}")]
public partial struct WindowId : IEquatable<WindowId>
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
