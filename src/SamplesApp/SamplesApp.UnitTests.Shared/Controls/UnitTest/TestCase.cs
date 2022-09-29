using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Uno.Extensions;

namespace Uno.UI.Samples.Tests;

internal record TestCase
{
	public object[] Parameters { get; init; } = Array.Empty<object>();

	public PointerDeviceType? Pointer { get; init; }

	/// <inheritdoc />
	public override string ToString()
	{
		var result = $"({Parameters.Select(p => p?.ToString() ?? "<null>").JoinBy(", ")})";

		if (Pointer is {} pt)
		{
			result += $" [{pt}]";
		}

		return result;
	}
}
