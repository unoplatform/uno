using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UITest.Helpers.Queries;

namespace Uno.UITest.Helpers.Queries;

internal static class SkiaAppExtensions
{
	public static async Task WaitForDependencyPropertyValueAsync(this SkiaApp app, QueryEx element, string dependencyPropertyName, string value)
	{
		const int interval = 25;
		const int attemps = 3000 / interval;

		for (var i = 0; i < attemps; i++)
		{
			var actual = element.GetDependencyPropertyValue<string>(dependencyPropertyName);
			if (actual?.Equals(value) ?? value is null)
			{
				return;
			}

			await Task.Delay(interval);
		}

		Assert.Fail($"Failed to get '{value}' for '{dependencyPropertyName}'.");
	}
}
