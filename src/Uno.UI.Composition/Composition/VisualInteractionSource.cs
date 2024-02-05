#nullable enable

using System;
using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

public partial class VisualInteractionSource : CompositionObject, ICompositionInteractionSource
{
	private VisualInteractionSourceRedirectionMode _manipulationRedirectionMode;

	private VisualInteractionSource(Visual source) : base(source.Compositor)
	{
		Source = source;
		ManipulationRedirectionMode = VisualInteractionSourceRedirectionMode.CapableTouchpadOnly;
	}

	/// <summary>
	/// The visual that is used for hit-testing and defines the co-ordinate space for gesture recognition.
	/// </summary>
	public Visual Source { get; }

	/// <summary>
	/// Defines how interactions are processed for an <see cref="VisualInteractionSource"/> on the scale axis.
	/// This property must be enabled to allow the <see cref="VisualInteractionSource"/> to send scale data to <see cref="InteractionTracker"/>.
	/// </summary>
	public InteractionSourceMode ScaleSourceMode { get; set; }

	/// <summary>
	/// Source mode for the X-axis.
	/// The <see cref="PositionXSourceMode"/> property defines how interactions are processed for a <see cref="VisualInteractionSource"/> on the X-axis.
	/// This property must be enabled to allow the <see cref="VisualInteractionSource"/> to send X-axis data to <see cref="InteractionTracker"/>.
	/// </summary>
	public InteractionSourceMode PositionXSourceMode { get; set; }

	/// <summary>
	/// Source mode for the Y-axis.
	/// The <see cref="PositionYSourceMode"/> property defines how interactions are processed for a <see cref="VisualInteractionSource"/> on the Y-axis.
	/// This property must be enabled to allow the <see cref="VisualInteractionSource"/> to send Y-axis data to <see cref="InteractionTracker"/>.
	/// </summary>
	public InteractionSourceMode PositionYSourceMode { get; set; }

	/// <summary>
	/// The <see cref="PositionXChainingMode"/> property defines the chaining behavior for an InteractionSource in the X direction.
	/// When chaining in the X direction is enabled, input will flow to the nearest ancestor's <see cref="VisualInteractionSource"/> whenever the
	/// interaction (such as panning) would otherwise take <see cref="InteractionTracker"/>'s position past its minimum or maximum X position.
	/// </summary>
	public InteractionChainingMode PositionXChainingMode { get; set; }

	/// <summary>
	/// The <see cref="PositionYChainingMode"/> property defines the chaining behavior for an InteractionSource in the Y direction.
	/// When chaining in the Y direction is enabled, input will flow to the nearest ancestor's <see cref="VisualInteractionSource"/> whenever the
	/// interaction (such as panning) would otherwise take <see cref="InteractionTracker"/>'s position past its minimum or maximum Y position.
	/// </summary>
	public InteractionChainingMode PositionYChainingMode { get; set; }

	/// <summary>
	/// Indicates what input should be redirected to the InteractionTracker.
	/// </summary>
	public VisualInteractionSourceRedirectionMode ManipulationRedirectionMode
	{
		get => _manipulationRedirectionMode;
		set
		{
			_manipulationRedirectionMode = value;

			// TODO: Mark the Source visual with correct values.
			// For now, we only support "Touch" (no Touchpad or PointerWheel support).
			//Source.IsTouchPadRedirected = (value & VisualInteractionSourceRedirectionMode.CapableTouchpadOnly) != 0;
			//Source.IsPointerWheelRedirected = (value & VisualInteractionSourceRedirectionMode.PointerWheelOnly) != 0;
		}
	}

	public static VisualInteractionSource Create(Visual source) => new VisualInteractionSource(source);

	// IMPORTANT: The correct API is Microsoft.UI.Input.PointerPoint!
	// Currently, Microsoft.UI.Input.PointerPoint is in Uno.UI assembly.
	// Uno.UI.Composition doesn't have access to Uno.UI (the dependency is the other way around).
	// For now, the API diverges from WinUI, and an implicit conversion operator will be provided to convert
	// from Microsoft.UI.Input.PointerPoint to Windows.UI.Input.PointerPoint.
	// In Uno 6, we will make a breaking change to match WinUI.
	public void TryRedirectForManipulation(Windows.UI.Input.PointerPoint pointerPoint)
	{
		if (pointerPoint.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
		{
			Source.RedirectTouchForPointerId(pointerPoint.PointerId);
		}
	}
}
