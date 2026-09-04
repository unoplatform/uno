using System;

namespace Uno;

internal interface IDispatcherTimer
{
	TimeSpan Interval { get; set; }

	bool IsEnabled { get; }

	event EventHandler<object> Tick;

	void Start();

	void Stop();
}
