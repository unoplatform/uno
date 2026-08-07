#nullable enable

using System;
using Microsoft.Windows.AppNotifications.Internal;
using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications;

[ContractVersion(typeof(AppNotificationsContract), 1 * 0x10000u)]
public sealed class AppNotification
{
	private readonly object _gate = new();
	private readonly string _payload;
	private string _tag = string.Empty;
	private string _group = string.Empty;
	private uint _id;
	private AppNotificationProgressData? _progress;
	private DateTimeOffset _expiration = DateTimeOffset.FromFileTime(0).ToLocalTime();
	private bool _expiresOnReboot;
	private AppNotificationPriority _priority;
	private bool _suppressDisplay;

	public AppNotification(string payload)
	{
		ArgumentNullException.ThrowIfNull(payload);
		AppNotificationPayloadParser.ValidateXml(payload);
		_payload = payload;
	}

	public string Tag
	{
		get
		{
			lock (_gate)
			{
				return _tag;
			}
		}
		set
		{
			lock (_gate)
			{
				_tag = value ?? string.Empty;
			}
		}
	}

	public string Group
	{
		get
		{
			lock (_gate)
			{
				return _group;
			}
		}
		set
		{
			lock (_gate)
			{
				_group = value ?? string.Empty;
			}
		}
	}

	public uint Id
	{
		get
		{
			lock (_gate)
			{
				return _id;
			}
		}
	}

	public string Payload => _payload;

	public AppNotificationProgressData? Progress
	{
		get
		{
			lock (_gate)
			{
				return _progress;
			}
		}
		set
		{
			lock (_gate)
			{
				_progress = value;
			}
		}
	}

	public DateTimeOffset Expiration
	{
		get
		{
			lock (_gate)
			{
				return _expiration;
			}
		}
		set
		{
			lock (_gate)
			{
				_expiration = value.ToLocalTime();
			}
		}
	}

	public bool ExpiresOnReboot
	{
		get
		{
			lock (_gate)
			{
				return _expiresOnReboot;
			}
		}
		set
		{
			lock (_gate)
			{
				_expiresOnReboot = value;
			}
		}
	}

	public AppNotificationPriority Priority
	{
		get
		{
			lock (_gate)
			{
				return _priority;
			}
		}
		set
		{
			lock (_gate)
			{
				_priority = value;
			}
		}
	}

	public bool SuppressDisplay
	{
		get
		{
			lock (_gate)
			{
				return _suppressDisplay;
			}
		}
		set
		{
			lock (_gate)
			{
				_suppressDisplay = value;
			}
		}
	}

	internal AppNotificationSnapshot CaptureSnapshot()
	{
		lock (_gate)
		{
			return new AppNotificationSnapshot(
				_id,
				_payload,
				_tag,
				_group,
				_expiration,
				_expiresOnReboot,
				_priority,
				_suppressDisplay,
				_progress?.Clone());
		}
	}

	internal void SetNotificationId(uint id)
	{
		lock (_gate)
		{
			_id = id;
		}
	}
}

internal sealed record AppNotificationSnapshot(
	uint Id,
	string Payload,
	string Tag,
	string Group,
	DateTimeOffset Expiration,
	bool ExpiresOnReboot,
	AppNotificationPriority Priority,
	bool SuppressDisplay,
	AppNotificationProgressData? Progress);
