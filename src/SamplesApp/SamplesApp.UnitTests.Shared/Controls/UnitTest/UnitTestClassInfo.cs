#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Uno.UI.Samples.Tests;

public class UnitTestClassInfo
{
	private const DynamicallyAccessedMemberTypes TypeRequirements = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
	public UnitTestClassInfo(
		[DynamicallyAccessedMembers(TypeRequirements)] Type? type,
		MethodInfo[]? tests,
		MethodInfo? initialize,
		MethodInfo? cleanup)
	{
		Type = type;
		TestClassName = Type?.Name ?? "(null)";
		Tests = tests;
		Initialize = initialize;
		Cleanup = cleanup;
	}

	public string TestClassName { get; }

	[DynamicallyAccessedMembers(TypeRequirements)]
	public Type? Type { get; }

	public MethodInfo[]? Tests { get; }

	public MethodInfo? Initialize { get; }

	public MethodInfo? Cleanup { get; }

	public override string ToString() => TestClassName;
}
