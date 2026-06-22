#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Helpers;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class ActivatableDataRowAttribute : DataRowAttribute
{
	internal const DynamicallyAccessedMemberTypes TypeRequirements = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

#pragma warning disable IDE0055
	// I don't know why IDE0055 is emitted for `data == null ? [type] : [..new object[]{type}, .. data]`
	public ActivatableDataRowAttribute([DynamicallyAccessedMembers(TypeRequirements)] Type type, params object?[]? data)
		: base(data == null ? [type] : [..new object[]{type}, .. data])
	{
	}

	public ActivatableDataRowAttribute([DynamicallyAccessedMembers(TypeRequirements)] string type, params object?[]? data)
		: base(data == null ? [type] : [..new object[]{type}, .. data])
	{
	}
#pragma warning restore IDE0055
}
