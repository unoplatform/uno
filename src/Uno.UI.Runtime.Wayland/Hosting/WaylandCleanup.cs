#nullable enable

using System;
using System.Collections.Generic;

namespace Uno.UI.Runtime.Wayland;

internal static class WaylandCleanup
{
	internal static void Run(params Action[] steps)
	{
		List<Exception>? errors = null;
		foreach (var step in steps)
		{
			try
			{
				step();
			}
			catch (Exception error)
			{
				(errors ??= new()).Add(error);
			}
		}
		if (errors is not null)
		{
			throw new AggregateException("Wayland resource cleanup failed.", errors);
		}
	}
}
