// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IXamlPredicate.g.h, commit 9d6fb15c0
//
// Note: WinUI exposes the parser-time conditional XAML predicates via the internal
// IXamlPredicate interface (single-argument list overload). Windows App SDK 2.0
// graduated this concept to a public API named IXamlCondition that takes a single
// string argument. Uno mirrors the public Windows App SDK 2.0 surface here.

namespace Microsoft.UI.Xaml.Markup
{
	/// <summary>
	/// Represents a custom XAML condition that is evaluated at XAML parse time and
	/// can be used from a conditional XAML namespace declaration to gate elements
	/// or attributes in markup.
	/// </summary>
	/// <remarks>
	/// The <see cref="Evaluate(string)"/> method is invoked by the XAML parser when
	/// it encounters a conditional namespace whose method name resolves to a type
	/// implementing <see cref="IXamlCondition"/>. The result for a given
	/// (condition type, argument) pair is cached for the lifetime of the process.
	/// </remarks>
	public partial interface IXamlCondition
	{
		/// <summary>
		/// Evaluates the condition for the supplied <paramref name="argument"/>.
		/// </summary>
		/// <param name="argument">
		/// The argument supplied to the conditional namespace, e.g. <c>NewExperience</c>
		/// in <c>xmlns:foo="...?local:FeatureFlagCondition(NewExperience)"</c>.
		/// </param>
		/// <returns>
		/// <c>true</c> if the gated markup should be included; otherwise, <c>false</c>.
		/// </returns>
		bool Evaluate(string argument);
	}
}
