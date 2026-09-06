namespace Windows.Devices.Geolocation
{
	public partial class StatusChangedEventArgs
	{
		internal StatusChangedEventArgs(PositionStatus status) =>
			Status = status;

		public PositionStatus Status { get; }
	}
}
