// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationProgressData.cpp, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System;

namespace Microsoft.Windows.AppNotifications;

partial class AppNotificationProgressData
{
	public AppNotificationProgressData(uint sequenceNumber)
	{
		ValidateSequenceNumber(sequenceNumber);
		_sequenceNumber = sequenceNumber;
	}

	public uint SequenceNumber
	{
		get
		{
			lock (_gate)
			{
				return _sequenceNumber;
			}
		}
		set
		{
			ValidateSequenceNumber(value);
			lock (_gate)
			{
				_sequenceNumber = value;
			}
		}
	}

	public string Title
	{
		get
		{
			lock (_gate)
			{
				return _title;
			}
		}
		set
		{
			lock (_gate)
			{
				_title = value ?? string.Empty;
			}
		}
	}

	public double Value
	{
		get
		{
			lock (_gate)
			{
				return _value;
			}
		}
		set
		{
			lock (_gate)
			{
				_value = value;
			}
		}
	}

	public string ValueStringOverride
	{
		get
		{
			lock (_gate)
			{
				return _valueStringOverride;
			}
		}
		set
		{
			lock (_gate)
			{
				_valueStringOverride = value ?? string.Empty;
			}
		}
	}

	public string Status
	{
		get
		{
			lock (_gate)
			{
				return _status;
			}
		}
		set
		{
			lock (_gate)
			{
				_status = value ?? string.Empty;
			}
		}
	}

	private static void ValidateSequenceNumber(uint sequenceNumber)
	{
		if (sequenceNumber == 0)
		{
			// The sequence number is always greater than 0
			throw new ArgumentException("The sequence number must be greater than zero.", nameof(sequenceNumber));
		}
	}
}
