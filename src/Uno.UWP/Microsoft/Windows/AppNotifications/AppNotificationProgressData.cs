#nullable enable

using System;

namespace Microsoft.Windows.AppNotifications;

public sealed class AppNotificationProgressData
{
	private readonly object _gate = new();
	private uint _sequenceNumber;
	private string _title = string.Empty;
	private double _value;
	private string _valueStringOverride = string.Empty;
	private string _status = string.Empty;

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
			throw new ArgumentException("The sequence number must be greater than zero.", nameof(sequenceNumber));
		}
	}
}
