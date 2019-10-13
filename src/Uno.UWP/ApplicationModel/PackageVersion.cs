namespace Windows.ApplicationModel
{
	public partial struct PackageVersion
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

		public ushort Major;
		public ushort Minor;
		public ushort Build;
		public ushort Revision;
	}
}
