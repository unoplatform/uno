#nullable enable

using System;
using System.Globalization;
using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications.Builder;

[ContractVersion(typeof(AppNotificationBuilderContract), 1 * 0x10000u)]
public sealed class AppNotificationProgressBar
{
	private BindMode _titleBindMode;
	private string _title = string.Empty;
	private BindMode _statusBindMode;
	private string _status = string.Empty;
	private BindMode _valueBindMode;
	private double _value;
	private BindMode _valueStringOverrideBindMode;
	private string _valueStringOverride = string.Empty;

	public string Title
	{
		get => _title;
		set
		{
			_title = value ?? string.Empty;
			_titleBindMode = BindMode.Value;
		}
	}

	public string Status
	{
		get => _status;
		set
		{
			_status = value ?? string.Empty;
			_statusBindMode = BindMode.Value;
		}
	}

	public double Value
	{
		get => _value;
		set
		{
			if (value < 0d || value > 1d)
			{
				throw new ArgumentException("The progress value must be between zero and one.", nameof(value));
			}

			_value = value;
			_valueBindMode = BindMode.Value;
		}
	}

	public string ValueStringOverride
	{
		get => _valueStringOverride;
		set
		{
			_valueStringOverride = value ?? string.Empty;
			_valueStringOverrideBindMode = BindMode.Value;
		}
	}

	public AppNotificationProgressBar SetTitle(string value)
	{
		Title = value;
		return this;
	}

	public AppNotificationProgressBar BindTitle()
	{
		_titleBindMode = BindMode.Bind;
		return this;
	}

	public AppNotificationProgressBar SetStatus(string value)
	{
		Status = value;
		return this;
	}

	public AppNotificationProgressBar BindStatus()
	{
		_statusBindMode = BindMode.Bind;
		return this;
	}

	public AppNotificationProgressBar SetValue(double value)
	{
		Value = value;
		return this;
	}

	public AppNotificationProgressBar BindValue()
	{
		_valueBindMode = BindMode.Bind;
		return this;
	}

	public AppNotificationProgressBar SetValueStringOverride(string value)
	{
		ValueStringOverride = value;
		return this;
	}

	public AppNotificationProgressBar BindValueStringOverride()
	{
		_valueStringOverrideBindMode = BindMode.Bind;
		return this;
	}

	internal string ToXml()
	{
		var title = _titleBindMode switch
		{
			BindMode.Value => $" title='{AppNotificationBuilderUtility.EncodeXml(_title)}'",
			BindMode.Bind => " title='{progressTitle}'",
			_ => string.Empty,
		};
		var status = _statusBindMode == BindMode.Value
			? $" status='{AppNotificationBuilderUtility.EncodeXml(_status)}'"
			: " status='{progressStatus}'";
		var value = _valueBindMode == BindMode.Value
			? $" value='{_value.ToString("G6", CultureInfo.InvariantCulture)}'"
			: " value='{progressValue}'";
		var valueStringOverride = _valueStringOverrideBindMode switch
		{
			BindMode.Value => $" valueStringOverride='{AppNotificationBuilderUtility.EncodeXml(_valueStringOverride)}'",
			BindMode.Bind => " valueStringOverride='{progressValueString}'",
			_ => string.Empty,
		};

		return $"<progress{title}{status}{value}{valueStringOverride}/>";
	}

	private enum BindMode
	{
		NotSet,
		Bind,
		Value,
	}
}
