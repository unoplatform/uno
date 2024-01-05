using System;

namespace Windows.ApplicationModel;

/// <summary>
/// Represents the package version info.
/// </summary>
public partial struct PackageVersion : IEquatable<PackageVersion>
{
	internal PackageVersion(ushort major)
	{
		Major = major;
		Minor = 0;
		Build = 0;
		Revision = 0;
	}

	internal PackageVersion(global::System.Version version)
	{
		Major = (ushort)(version.Major >= 0 ? version.Major : 0);
		Minor = (ushort)(version.Minor >= 0 ? version.Minor : 0);
		Build = (ushort)(version.Build >= 0 ? version.Build : 0);
		Revision = (ushort)(version.Revision >= 0 ? version.Revision : 0);
	}

#if HAS_UNO_WINUI
	public PackageVersion(ushort _Major, ushort _Minor, ushort _Build, ushort _Revision)
	{
		Major = _Major;
		Minor = _Minor;
		Build = _Build;
		Revision = _Revision;
	}
#endif

	// NOTE: Equality implementation should be modified if a new field/property is added.

	/// <summary>
	/// The major version number of the package.
	/// </summary>
	public ushort Major;

	/// <summary>
	/// The minor version number of the package.
	/// </summary>
	public ushort Minor;

	/// <summary>
	/// The build version number of the package.
	/// </summary>
	public ushort Build;

	/// <summary>
	/// The revision version number of the package.
	/// </summary>
	public ushort Revision;

	#region Equality Members
	public override bool Equals(object? obj) => obj is PackageVersion version && Equals(version);
	public bool Equals(PackageVersion other) => Major == other.Major && Minor == other.Minor && Build == other.Build && Revision == other.Revision;

	public override int GetHashCode()
	{
		var hashCode = -1452750829;
		hashCode = hashCode * -1521134295 + Major.GetHashCode();
		hashCode = hashCode * -1521134295 + Minor.GetHashCode();
		hashCode = hashCode * -1521134295 + Build.GetHashCode();
		hashCode = hashCode * -1521134295 + Revision.GetHashCode();
		return hashCode;
	}

	public static bool operator ==(PackageVersion left, PackageVersion right) => left.Equals(right);
	public static bool operator !=(PackageVersion left, PackageVersion right) => !left.Equals(right);
	#endregion
}
