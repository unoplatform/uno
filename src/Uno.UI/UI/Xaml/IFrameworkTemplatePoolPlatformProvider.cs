#nullable enable

using System;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml;

internal interface IFrameworkTemplatePoolPlatformProvider
{
	TimeSpan Now { get; }

	void Schedule(Action action);

	Task Delay(TimeSpan duration);

	bool CanUseMemoryManager { get; }

	ulong AppMemoryUsage { get; }

	ulong AppMemoryUsageLimit { get; }
}
