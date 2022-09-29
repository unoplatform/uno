#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno
{
	internal interface IDispatcherTimer
	{
		TimeSpan Interval { get; set; }
		bool IsEnabled { get; }

		event EventHandler<object> Tick;

		void Start();
		void Stop();
	}
}
