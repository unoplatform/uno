// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotification.cpp, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System;
using Microsoft.Windows.AppNotifications.Internal;

namespace Microsoft.Windows.AppNotifications;

partial class AppNotification
{
	public AppNotification(string payload)
	{
		ArgumentNullException.ThrowIfNull(payload);
		// We call LoadXml to verify the payload is xml
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

	internal void SetNotificationId(uint id)
	{
		lock (_gate)
		{
			_id = id;
		}
	}
}
