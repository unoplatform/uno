#if !IS_RUNTIME_UI_TESTS

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Uno.UITest.Helpers.Queries;

public static class AppExtensions
{
	// Async versions of the Uno.UITest.Helpers.Queries.QueryExtensions
	// Those are only mapping to the sync version but are needed to allow
	// multi-targeting with **Runtime UI tests** engine which has only async versions.

	public static async Task WaitForDependencyPropertyValueAsync(
		this IApp app,
		QueryEx element,
		string dependencyPropertyName,
		string value)
		=> app.WaitForDependencyPropertyValue(element, dependencyPropertyName, value);

	public static async Task WaitForDependencyPropertyValueAsync<T>(
		this IApp app,
		Func<IAppQuery, IAppQuery> element,
		string dependencyPropertyName,
		T expectedValue)
		=> app.WaitForDependencyPropertyValue<T>(element, dependencyPropertyName, expectedValue);
}
#endif
