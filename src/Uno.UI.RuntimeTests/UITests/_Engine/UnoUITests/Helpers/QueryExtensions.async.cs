using System;
using System.Linq;
using System.Threading.Tasks;

namespace Uno.UITest.Helpers.Queries;

internal static partial class QueryExtensions
{
	// This class contains **ASYNC** extensions method that match a sync version of the Uno.UITest.Helpers.Queries.QueryExtensions contract.
	// When a method is added here, a correspondent version should be added in \src\SamplesApp\SamplesApp.UITests\Extensions\QueryExtensions.async.cs
	// to allow usage in the Xamarin / Selenium UI tests.

	public static async Task WaitForDependencyPropertyValueAsync(this IApp app, QueryEx element, string dependencyPropertyName, string value)
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

	public static async Task WaitForDependencyPropertyValueAsync<T>(
		this IApp app,
		Func<IAppQuery, IAppQuery> element,
		string dependencyPropertyName,
		T expectedValue)
	{
		const int interval = 25;
		const int attemps = 3000 / interval;

		for (var i = 0; i < attemps; i++)
		{
			var actual = new QueryEx(element).GetDependencyPropertyValue<T>(dependencyPropertyName);
			if (actual?.Equals(expectedValue) ?? expectedValue is null)
			{
				return;
			}

			await Task.Delay(interval);
		}

		Assert.Fail($"Failed to get '{expectedValue}' for '{dependencyPropertyName}'.");
	}
}
