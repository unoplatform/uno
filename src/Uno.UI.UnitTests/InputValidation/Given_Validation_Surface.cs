#nullable enable

using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.InputValidation;

/// <summary>
/// Guards the accessibility of the input validation participation surface on <see cref="Control"/>.
/// </summary>
/// <remarks>
/// This proves the modifier, not the sufficiency of the surface: a member that stays internal somewhere else
/// would leave this green while a third-party control still could not be written. The proof of sufficiency is
/// that <c>Uno.UI.Tests.ViewLibrary</c> compiles at all — it is outside Uno.UI's InternalsVisibleTo. This test
/// exists for the legible failure message when one of these is accidentally narrowed, so do not delete the
/// ViewLibrary control believing this covers it.
/// </remarks>
[TestClass]
public class Given_Validation_Surface
{
	// Spelled out rather than via nameof: a protected member is not accessible from a type that does not
	// derive from Control, which is the whole point of the guard.
	private static readonly string[] _participationMembers =
	[
		"OnInputValidationModeChanged",
		"OnInputValidationKindChanged",
		"OnHasValidationErrorsChanged",
		"OnErrorTemplateChanged",
		"GetOrCreateValidationErrors",
		"AddHasValidationErrorsChangedHandler",
		"RemoveHasValidationErrorsChangedHandler",
		"AddValidationErrorHandler",
		"RemoveValidationErrorHandler",
		"AddErrorChangedHandler",
		"RemoveErrorChangedHandler",
	];

	[TestMethod]
	public void When_Participation_Members_Are_Protected()
	{
		foreach (var name in _participationMembers)
		{
			var method = typeof(Control)
				.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
				.SingleOrDefault(m => m.Name == name);

			Assert.IsNotNull(method, $"Control.{name} is missing. A control outside Uno.UI registers against it.");
			Assert.IsTrue(
				method.IsFamily,
				$"Control.{name} must stay protected: a control outside Uno.UI cannot reach it otherwise.");
		}
	}
}
