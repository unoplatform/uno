using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	[ContractVersion(typeof(UniversalApiContract), 65536U)]
	[Flags]
	public enum GestureSettings : uint
	{
		/// <summary>Disable support for gestures and manipulations.</summary>
		None = 0U,
		/// <summary>Enable support for the tap gesture.</summary>
		Tap = 1U,
		/// <summary>Enable support for the double-tap gesture.</summary>
		DoubleTap = 2U,
		/// <summary>Enable support for the press and hold gesture (from a single touch or pen/stylus contact). The Holding event is raised if a time threshold is crossed before the contact is lifted, an additional contact is detected, or a gesture is started.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		Hold = 4U,
		/// <summary>Enable support for the press and hold gesture through the left button on a mouse. The Holding event is raised if a time threshold is crossed before the left button is released or a gesture is started.This gesture can be used to display a context menu.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		HoldWithMouse = 8U,
		/// <summary>Enable support for a right-tap interaction. The RightTapped event is raised when the contact is lifted or the mouse button released.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		RightTap = 16U,
		/// <summary>Enable support for the slide or swipe gesture with a mouse or pen/stylus (single contact). The Dragging event is raised when either gesture is detected.This gesture can be used for text selection, selecting or rearranging objects, or scrolling and panning.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		Drag = 32U,
		/// <summary>Enable support for the slide gesture through pointer input, on the horizontal axis. The ManipulationStarted, ManipulationUpdated, and ManipulationCompleted events are all raised during the course of this interaction.This gesture can be used for rearranging objects.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		ManipulationTranslateX = 64U,
		/// <summary>Enable support for the slide gesture through pointer input, on the vertical axis. The ManipulationStarted, ManipulationUpdated, and ManipulationCompleted events are all raised during the course of this interaction.This gesture can be used for rearranging objects.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		ManipulationTranslateY = 128U,
		/// <summary>Enable support for the slide gesture through pointer input, on the horizontal axis using rails (guides). The ManipulationStarted, ManipulationUpdated, and ManipulationCompleted events are all raised during the course of this interaction.This gesture can be used for rearranging objects.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		ManipulationTranslateRailsX = 256U,
		/// <summary>Enable support for the slide gesture through pointer input, on the vertical axis using rails (guides). The ManipulationStarted, ManipulationUpdated, and ManipulationCompleted events are all raised during the course of this interaction.This gesture can be used for rearranging objects.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		ManipulationTranslateRailsY = 512U,
		/// <summary>Enable support for the rotation gesture through pointer input. The ManipulationStarted, ManipulationUpdated, and ManipulationCompleted events are all raised during the course of this interaction.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		ManipulationRotate = 1024U,
		/// <summary>Enable support for the pinch or stretch gesture through pointer input.These gestures can be used for optical or semantic zoom and resizing an object. The ManipulationStarted, ManipulationUpdated, and ManipulationCompleted events are all raised during the course of this interaction.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		ManipulationScale = 2048U,
		/// <summary>Enable support for translation inertia after the slide gesture (through pointer input) is complete. The ManipulationInertiaStarting event is raised if inertia is enabled.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		ManipulationTranslateInertia = 4096U,
		/// <summary>Enable support for rotation inertia after the rotate gesture (through pointer input) is complete. The ManipulationInertiaStarting event is raised if inertia is enabled.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		ManipulationRotateInertia = 8192U,
		/// <summary>Enable support for scaling inertia after the pinch or stretch gesture (through pointer input) is complete. The ManipulationInertiaStarting event is raised if inertia is enabled.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		ManipulationScaleInertia = 16384U,
		/// <summary>Enable support for the CrossSliding interaction when using the slide or swipe gesture through a single touch contact.This gesture can be used for selecting or rearranging objects.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		CrossSlide = 32768U,
		/// <summary>Enable panning and disable zoom when two or more touch contacts are detected.Prevents unintentional zoom interactions when panning with multiple fingers.</summary>
		[global::Uno.NotImplemented] // The GestureRecognizer won't raise this event
		[ContractVersion("Windows.Foundation.UniversalApiContract", 65536U)] ManipulationMultipleFingerPanning = 65536U,
	}
}
