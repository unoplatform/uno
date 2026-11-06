// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Tests.Common;

public sealed class ValidateTreeParams
{
	// We set IsHitTestVisible=False on the root element to prevent mouse interference.
	// By default, we exclude the FocusState property because depending on in
	// which order the test is executed Controls may or may not have it set,
	// leading to instability.
	// We also exclude the AutomationProperties.ControlledPeers property.
	// This is an accessibility property set on RITP machines but nowhere else on our testing environment.
	// IsPressed and IsPointerOver frequently cause tests to fail due to sometimes being False and sometimes being
	// "empty" (i.e. unset). These properties are not important for the purposes of validating control states, so we ignore them.
	public const string DefaultUIElementTreeValidationRules =
		"""
		<?xml version='1.0' encoding='UTF-8'?>
		            <Rules>
		                <Rule Applicability='/VisualTreeDump/VisualRoot/Element' Inclusion='Blacklist'>
		                    <Property Name='IsHitTestVisible'/>
		                </Rule>
		                <Rule Applicability='//Element' Inclusion='Blacklist'>
		                    <Property Name='FocusState'/>
		                    <Property Name='AutomationProperties.ControlledPeers'/>
		                    <Property Name='IsPressed'/>
		                    <Property Name='IsPointerOver'/>
		                </Rule>
		            </Rules>
		""";

	public ValidateTreeParams(
		Size windowSizeOverride,
		float scaleFactorOverride,
		Func<Panel> setupFunc)
		: this(
			"",
			windowSizeOverride,
			scaleFactorOverride,
			setupFunc,
			null,
			false,
			DefaultUIElementTreeValidationRules,
			true)
	{
	}

	public ValidateTreeParams(
		Size windowSizeOverride,
		float scaleFactorOverride,
		Func<Panel> setupFunc,
		Action cleanupFunc)
		: this(
			"",
			windowSizeOverride,
			scaleFactorOverride,
			setupFunc,
			cleanupFunc,
			false,
			DefaultUIElementTreeValidationRules,
			true)
	{
	}

	public ValidateTreeParams(
		Size windowSizeOverride,
		float scaleFactorOverride,
		Func<Panel> setupFunc,
		Action cleanupFunc,
		bool disableHittestingOnRoot,
		bool ignorePopups = false)
		: this(
			"",
			windowSizeOverride,
			scaleFactorOverride,
			setupFunc,
			cleanupFunc,
			false,
			DefaultUIElementTreeValidationRules,
			disableHittestingOnRoot,
			ignorePopups)
	{
	}

	public ValidateTreeParams(
		string subTestId,
		Size windowSizeOverride,
		float scaleFactorOverride,
		Func<Panel> setupFunc)
		: this(
			subTestId,
			windowSizeOverride,
			scaleFactorOverride,
			setupFunc,
			null,
			false,
			DefaultUIElementTreeValidationRules,
			true)
	{
	}

	public ValidateTreeParams(
		string subTestId,
		Size windowSizeOverride,
		float scaleFactorOverride,
		Func<Panel> setupFunc,
		string validationRules)
		: this(
			subTestId,
			windowSizeOverride,
			scaleFactorOverride,
			setupFunc,
			null,
			false,
			validationRules,
			true)
	{
	}

	public ValidateTreeParams(
		string subTestId,
		Size windowSizeOverride,
		float scaleFactorOverride,
		Func<Panel> setupFunc,
		Action cleanupFunc,
		bool disableHittestingOnRoot = true,
		bool ignorePopups = false)
		: this(
			subTestId,
			windowSizeOverride,
			scaleFactorOverride,
			setupFunc,
			cleanupFunc,
			false,
			DefaultUIElementTreeValidationRules,
			disableHittestingOnRoot,
			ignorePopups)
	{
	}

	public ValidateTreeParams(
		string subTestId,
		Size windowSizeOverride,
		float scaleFactorOverride,
		Func<Panel> setupFunc,
		Action cleanupFunc,
		bool skipHCColorValidationOnMasterMismatch,
		string validationRules,
		bool disableHittestingOnRoot = true,
		bool ignorePopups = false)
	{
		SubTestId = subTestId;
		WindowSizeOverride = windowSizeOverride;
		ScaleFactorOverride = scaleFactorOverride;
		SetupFunc = setupFunc;
		CleanupFunc = cleanupFunc;
		SkipHCColorValidationOnMasterMismatch = skipHCColorValidationOnMasterMismatch;
		ValidationRules = validationRules;
		DisableHittestingOnRoot = disableHittestingOnRoot;
		IgnorePopups = ignorePopups;
	}

	public string SubTestId { get; set; }

	public Size WindowSizeOverride { get; set; }
	public float ScaleFactorOverride { get; set; }

	// The SetupFunc function object should configure a visual tree that represents all
	// the states of a control that need to be validated.  It is responsible for setting
	// that tree as the WindowContent.
	public Func<Panel> SetupFunc { get; set; }
	public Action CleanupFunc { get; set; }

	public bool SkipHCColorValidationOnMasterMismatch { get; set; }

	// Set IsHitTestVisible=False on the root visual if this is true.
	public bool DisableHittestingOnRoot { get; set; }

	public string ValidationRules { get; set; }

	public bool IgnorePopups { get; set; }
}
