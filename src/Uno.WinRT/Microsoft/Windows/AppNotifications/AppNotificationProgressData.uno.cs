#nullable enable

namespace Microsoft.Windows.AppNotifications;

partial class AppNotificationProgressData
{
	internal AppNotificationProgressData Clone()
	{
		lock (_gate)
		{
			return new AppNotificationProgressData(_sequenceNumber)
			{
				Title = _title,
				Value = _value,
				ValueStringOverride = _valueStringOverride,
				Status = _status,
			};
		}
	}
}
