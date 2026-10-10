#nullable enable

using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.InputValidation;

/// <summary>
/// Guards the accessibility of the input validation surface on <see cref="Control"/>.
/// </summary>
/// <remarks>
/// A control outside Uno.UI participates by declaring its input, and drives the validation states from its
/// own visual state code through <c>UpdateValidationStates</c>. Everything else is reached from the
/// <c>Uno.Extras.Input.Validation</c> attached properties, through InternalsVisibleTo.
/// </remarks>
[TestClass]
public class Given_Validation_Surface
{
	[TestMethod]
	public void When_UpdateValidationStates_Is_Protected()
	{
		// Spelled out rather than via nameof: a protected member is not accessible from a type that does not
		// derive from Control, which is the whole point of the guard.
		var method = typeof(Control)
			.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.SingleOrDefault(m => m.Name == "UpdateValidationStates");

		Assert.IsNotNull(method, "Control.UpdateValidationStates is missing.");
		Assert.IsTrue(
			method.IsFamily,
			"Control.UpdateValidationStates must stay protected: a control outside Uno.UI cannot reach it otherwise.");
	}
}
