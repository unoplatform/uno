// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference TextSelectionSettings.h, TextSelectionSettings.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

//---------------------------------------------------------------------------
//
//  TextSelectionSettings
//
//  Singleton-like. Contains the hard coded parameters.
//
//---------------------------------------------------------------------------

// TODO (OS TFS 11359798): None of these values ever change, so these should be changed to just be constants without a class instance.
internal sealed class TextSelectionSettings
{
	public static TextSelectionSettings Get() => m_Instance;

	public float m_rHandleTopToCenterHeight;          // The distance between the top of the handle and the center point of the gripper.
													  // This value is used to adjust the safety zone processing vs the selection location.
	public float m_rHandleInternalOutlineDiameter;    // Diameter of the internal outline of the handle in millimeters
	public float m_rHandleExternalOutlineDiameter;    // Diameter of the external outline of the handle in millimeters
	public float m_rHandleOutlineThickness;           // Thickness of the internal outline of the handle in millimeters
	public float m_rHandlePostThickness;              // Thickness of the handle's post in millimeters
	public float m_rHandleTouchTargetWidth;           // Width of the rectangular touch target of handle in millimeters
	public float m_rHandleTouchTargetHeight;          // Height of the rectangular touch target of handle in millimeters
	public float m_rCaretAnimationRampUpFraction;     // Fraction of the caret blink interval when the caret fades in [0..1]
	public float m_rCaretAnimationRampDownFraction;   // Fraction of the caret blink interval when the caret fades out [0..1]
	public float m_rCaretAnimationUpPlateauFraction;  // Fraction of the caret blink interval when the caret stays on [0..1]
	public float m_rCaretAnimationDownPlateauFraction;// Fraction of the caret blink interval when the caret stays off [0..1]
	public float m_rCaretAnimationMaxBlockOpacity;    // Maximum opacity of the caret when it is wider than 1 pixel [0..1]
	public float m_rCaretBlinkTimeout;                // The caret will stop this many seconds. When <= 0, the timeout is disabled
	public float m_rCaretBlinkStart;                  // The caret will wait this many seconds before it starts blinking

	private TextSelectionSettings()
	{
		SetDefaults();
	}

	//------------------------------------------------------------------------
	//
	//  Resets the settings to default values. Used in constructor and
	//  to reset values in case of an error.
	//
	//------------------------------------------------------------------------
	private void SetDefaults()
	{
		m_rHandleTopToCenterHeight = 0.6f;
		m_rHandleInternalOutlineDiameter = 3.7f;
		m_rHandleExternalOutlineDiameter = 4.8f;
		m_rHandleOutlineThickness = 0.4f;
		m_rHandlePostThickness = 0.6f;
		m_rHandleTouchTargetWidth = 8.0f; // TODO: this the next one are smaller
		m_rHandleTouchTargetHeight = 9.3f; // than Win8 (12.0f). Check with PM.
		m_rCaretAnimationRampUpFraction = 0.12f;
		m_rCaretAnimationRampDownFraction = 0.12f;
		m_rCaretAnimationUpPlateauFraction = 0.38f;
		m_rCaretAnimationDownPlateauFraction = 0.38f;
		m_rCaretAnimationMaxBlockOpacity = 0.7f;
		m_rCaretBlinkTimeout = 5.0f;
		m_rCaretBlinkStart = 1.0f;
	}

	private static readonly TextSelectionSettings m_Instance = new(); // Instead of allocating an object, will use a static instance
}
