#nullable enable

using System;
using System.Threading.Tasks;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml;

internal interface IFrameworkTemplatePoolPlatformProvider
{
	TimeSpan Now { get; }

	void Schedule(IdleDispatchedHandler action);

	Task Delay(TimeSpan duration);

	bool CanUseMemoryManager { get; }

	ulong AppMemoryUsage { get; }

	ulong AppMemoryUsageLimit { get; }
}
