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

	public bool RequiresFullWindow { get; }

	public float? RequiresScaling { get; }

	public bool RunsOnUIThread { get; }

	public bool PassFiltersAsFirstParameter { get; }

	private bool HasCustomAttribute<T>(MemberInfo? testMethod)
		=> testMethod?.GetCustomAttribute(typeof(T)) != null;

	// Adopted from https://github.com/microsoft/testfx/blob/47dee826a0a3eb7a2d9d089ed8aba9d2dabfe82e/src/Adapter/MSTest.TestAdapter/Helpers/AttributeHelpers.cs
	private static bool IsIgnored(ICustomAttributeProvider member, out string? ignoreMessage)
	{
		var attributes = member.GetCustomAttributes(typeof(ConditionBaseAttribute), inherit: false).Cast<ConditionBaseAttribute>();
		IEnumerable<IGrouping<string, ConditionBaseAttribute>> groups = attributes.GroupBy(attr => attr.GroupName);
		foreach (IGrouping<string, ConditionBaseAttribute>? group in groups)
		{
			bool atLeastOneInGroupIsSatisfied = false;
			string? firstNonSatisfiedMatch = null;
			foreach (ConditionBaseAttribute attribute in group)
			{
				bool shouldRun = attribute.Mode == ConditionMode.Include ? attribute.IsConditionMet : !attribute.IsConditionMet;
				if (shouldRun)
				{
					atLeastOneInGroupIsSatisfied = true;
					break;
				}

				firstNonSatisfiedMatch ??= attribute.IgnoreMessage;
			}

			if (!atLeastOneInGroupIsSatisfied)
			{
				ignoreMessage = firstNonSatisfiedMatch;
				return true;
			}
		}

		ignoreMessage = null;
		return false;
	}

	public bool IsIgnored(out string? ignoreMessage)
	{
		bool shouldIgnoreClass = IsIgnored(Method, out string? ignoreMessageOnClass);
		bool shouldIgnoreMethod = IsIgnored(Method.DeclaringType!, out string? ignoreMessageOnMethod);
		ignoreMessage = ignoreMessageOnClass;
		if (string.IsNullOrEmpty(ignoreMessage) && shouldIgnoreMethod)
		{
			ignoreMessage = ignoreMessageOnMethod;
		}

		return shouldIgnoreClass || shouldIgnoreMethod;
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
