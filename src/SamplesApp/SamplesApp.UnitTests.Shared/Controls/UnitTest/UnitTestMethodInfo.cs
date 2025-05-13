#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.Devices.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Testing;
using Uno.UI.RuntimeTests;

namespace Uno.UI.Samples.Tests;

internal record UnitTestMethodInfo
{
	private readonly List<object?[]> _casesParameters;
	private readonly IList<PointerDeviceType> _injectedPointerTypes;

	private readonly bool _ignoredBecauseOfConditionalTestAttribute;

	public UnitTestMethodInfo(object testClassInstance, MethodInfo method)
	{
		Method = method;
		RunsOnUIThread = testClassInstance is SamplesApp.UITests.SampleControlUITestBase ||
			HasCustomAttribute<RunsOnUIThreadAttribute>(method) ||
			HasCustomAttribute<RunsOnUIThreadAttribute>(method.DeclaringType);
		RequiresFullWindow =
			HasCustomAttribute<RequiresFullWindowAttribute>(method) ||
			HasCustomAttribute<RequiresFullWindowAttribute>(method.DeclaringType);

		var requiresScalingAttribute = method.GetCustomAttribute<RequiresScalingAttribute>() ?? method.DeclaringType?.GetCustomAttribute<RequiresScalingAttribute>();
		if (requiresScalingAttribute is not null)
		{
			RequiresScaling = requiresScalingAttribute.Scaling;
		}

		PassFiltersAsFirstParameter =
			HasCustomAttribute<FiltersAttribute>(method) ||
			HasCustomAttribute<FiltersAttribute>(method.DeclaringType);
		ExpectedException = method
			.GetCustomAttributes<ExpectedExceptionAttribute>()
			.SingleOrDefault()
			?.ExceptionType;

		var ignoredBecauseOfConditionalTestClassAttribute = method.DeclaringType?
			.GetCustomAttributes<ConditionalTestClassAttribute>()
			.SingleOrDefault()
			?.ShouldRun() == false;

		var ignoredBecauseOfConditionalTestAttribute = method
			.GetCustomAttributes<ConditionalTestAttribute>()
			.SingleOrDefault()
			?.ShouldRun() == false;

		_ignoredBecauseOfConditionalTestAttribute = ignoredBecauseOfConditionalTestClassAttribute | ignoredBecauseOfConditionalTestAttribute;

		_casesParameters = method
			.GetCustomAttributes<Attribute>()
			.OfType<ITestDataSource>()
			.SelectMany(d => d.GetData(method))
			.ToList();

		if (_casesParameters is { Count: 0 })
		{
			_casesParameters.Add(Array.Empty<object>());
		}

		_injectedPointerTypes = method
			.GetCustomAttributes<InjectedPointerAttribute>()
			.Select(attr => attr.Type)
			.Distinct()
			.ToList();
	}

	public string Name => Method.Name;

	public MethodInfo Method { get; }

	public Type? ExpectedException { get; }

	public bool RequiresFullWindow { get; }

	public float? RequiresScaling { get; }

	public bool RunsOnUIThread { get; }

	public bool PassFiltersAsFirstParameter { get; }

	private bool HasCustomAttribute<T>(MemberInfo? testMethod)
		=> testMethod?.GetCustomAttribute(typeof(T)) != null;

	public bool IsIgnored(out string ignoreMessage)
	{
		var ignoreAttribute = Method.GetCustomAttribute<IgnoreAttribute>();
		if (ignoreAttribute == null)
		{
			ignoreAttribute = Method.DeclaringType?.GetCustomAttribute<IgnoreAttribute>();
		}

		if (ignoreAttribute != null)
		{
			ignoreMessage = string.IsNullOrEmpty(ignoreAttribute.IgnoreMessage) ? "Test is marked as ignored" : ignoreAttribute.IgnoreMessage;
			return true;
		}

		if (_ignoredBecauseOfConditionalTestAttribute)
		{
			ignoreMessage = "The test is ignored on the current platform";
			return true;
		}

		ignoreMessage = "";
		return false;
	}

	public IEnumerable<TestCase> GetCases()
	{
		var cases = _casesParameters.Select(parameters => new TestCase { Parameters = parameters });

		if (_injectedPointerTypes.Any())
		{
			var currentCases = cases.ToList();
			cases = _injectedPointerTypes.SelectMany(pointer => currentCases.Select(testCase => testCase with { Pointer = pointer }));
		}

		return cases;
	}
}
