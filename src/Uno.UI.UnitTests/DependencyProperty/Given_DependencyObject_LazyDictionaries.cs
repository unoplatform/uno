// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.DependencyPropertyTests;

/// <summary>
/// Regression tests for issue #24752: DependencyObject should not eagerly allocate
/// _inheritedForwardedProperties and _propertyChangedTokens dictionaries.
/// </summary>
[TestClass]
public class Given_DependencyObject_LazyDictionaries
{
	private static readonly FieldInfo s_inheritedForwardedPropertiesField =
		typeof(DependencyObject).GetField(
			"_inheritedForwardedProperties",
			BindingFlags.NonPublic | BindingFlags.Instance)
		?? throw new InvalidOperationException("Field '_inheritedForwardedProperties' not found on DependencyObject.");

	private static readonly FieldInfo s_propertyChangedTokensField =
		typeof(DependencyObject).GetField(
			"_propertyChangedTokens",
			BindingFlags.NonPublic | BindingFlags.Instance)
		?? throw new InvalidOperationException("Field '_propertyChangedTokens' not found on DependencyObject.");

	/// <summary>
	/// Verifies that both lazy dictionaries remain null immediately after construction,
	/// before any property-system operation touches them.
	/// </summary>
	[TestMethod]
	public void When_Constructed_Then_LazyDictionaries_AreNull()
	{
		var brush = new SolidColorBrush();

		var inheritedForwardedProperties = s_inheritedForwardedPropertiesField.GetValue(brush);
		var propertyChangedTokens = s_propertyChangedTokensField.GetValue(brush);

		Assert.IsNull(
			inheritedForwardedProperties,
			"_inheritedForwardedProperties should be null until first inherited forwarded property is set.");

		Assert.IsNull(
			propertyChangedTokens,
			"_propertyChangedTokens should be null until RegisterPropertyChangedCallback is first called.");
	}

	/// <summary>
	/// Verifies that _propertyChangedTokens is allocated on the first call to
	/// RegisterPropertyChangedCallback and is null before that call.
	/// </summary>
	[TestMethod]
	public void When_RegisterPropertyChangedCallback_Then_TokenDictionary_IsAllocated()
	{
		var brush = new SolidColorBrush();

		// Pre-condition: null before any callback registration.
		Assert.IsNull(s_propertyChangedTokensField.GetValue(brush));

		long token = brush.RegisterPropertyChangedCallback(
			SolidColorBrush.ColorProperty,
			(_, _) => { });

		// Post-condition: dictionary was created.
		Assert.IsNotNull(
			s_propertyChangedTokensField.GetValue(brush),
			"_propertyChangedTokens should be allocated after the first RegisterPropertyChangedCallback call.");

		// Cleanup.
		brush.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, token);
	}

	/// <summary>
	/// Verifies that UnregisterPropertyChangedCallback with an unknown token (before any
	/// registration has occurred) does not throw a NullReferenceException even when the
	/// dictionary is still null.
	/// </summary>
	[TestMethod]
	public void When_UnregisterWithNoRegistrations_Then_NoException()
	{
		var brush = new SolidColorBrush();

		// Dictionary is null — this must not throw.
		brush.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, token: 9999L);

		Assert.IsNull(
			s_propertyChangedTokensField.GetValue(brush),
			"_propertyChangedTokens should remain null when no registrations were ever made.");
	}
}
