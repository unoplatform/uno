using System;
using System.Linq;

namespace Windows.Devices.Input;

internal readonly struct PointerIdentifier : IEquatable<PointerIdentifier>
{
	private readonly long _uid;

	public PointerIdentifier(PointerDeviceType type, uint id)
	{
		_uid = (long)type << 32 | id;
	}

	public uint Id => unchecked((uint)(_uid & 0xFFFF_FFFF));

	public PointerDeviceType Type => unchecked((PointerDeviceType)(_uid >> 32));

	/// <inheritdoc />
	public override string ToString()
		=> $"{Type}/{Id}";

	/// <inheritdoc />
	public override int GetHashCode()
		=> _uid.GetHashCode();

	/// <inheritdoc />
	public override bool Equals(object? obj)
		=> obj is PointerIdentifier other && Equals(this, other);

	/// <inheritdoc />
	public bool Equals(PointerIdentifier other)
		=> Equals(this, other);

	private static bool Equals(PointerIdentifier left, PointerIdentifier right)
		=> left._uid == right._uid;

	public static bool operator ==(PointerIdentifier left, PointerIdentifier right)
		=> Equals(left, right);

	public static bool operator !=(PointerIdentifier left, PointerIdentifier right)
		=> !Equals(left, right);

	public static implicit operator long(PointerIdentifier identifier)
		=> identifier._uid;
}
