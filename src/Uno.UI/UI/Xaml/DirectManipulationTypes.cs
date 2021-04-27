using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.UI.Xaml
{
	// Keep this enum in sync with the DirectManipulationState enum in DirectManipulationContainer.h
	// Manipulation states
	enum DMManipulationState
	{
		DMManipulationStarting = 1,
		DMManipulationStarted = 2,
		DMManipulationDelta = 3,
		DMManipulationLastDelta = 4,
		DMManipulationCompleted = 5,
		ConstantVelocityScrollStarted = 6,
		ConstantVelocityScrollStopped = 7
	};

	// Keep in sync with XDMAlignment in PalTypes.h
	enum DMAlignment
	{
		DMAlignmentNone = 0x00,
		DMAlignmentNear = 0x01,
		DMAlignmentCenter = 0x02,
		DMAlignmentFar = 0x04,
		DMAlignmentUnlockCenter = 0x08
	};

	// Keep in sync with XDMConfigurations in PalTypes.h
	// Viewport configurations
	enum DMConfigurations
	{
		DMConfigurationNone = 0x00,
		DMConfigurationInteraction = 0x01,
		DMConfigurationPanX = 0x02,
		DMConfigurationPanY = 0x04,
		DMConfigurationZoom = 0x10,
		DMConfigurationPanInertia = 0x20,
		DMConfigurationZoomInertia = 0x80,
		DMConfigurationRailsX = 0x100,
		DMConfigurationRailsY = 0x200
	};

	// Keep in sync with XDMMotionTypes in PalTypes.h
	// Content motion types
	enum DMMotionTypes
	{
		DMMotionTypeNone = 0x00,
		DMMotionTypePanX = 0x01,
		DMMotionTypePanY = 0x02,
		DMMotionTypeZoom = 0x04,
		DMMotionTypeCenterX = 0x10,
		DMMotionTypeCenterY = 0x20
	};

	// Keep in sync with XDMSnapCoordinate in PalTypes.h
	enum DMSnapCoordinate
	{
		DMSnapCoordinateBoundary = 0x00,
		DMSnapCoordinateOrigin = 0x01,
		DMSnapCoordinateMirrored = 0x10
	};

	// Keep in sync with XDMContentType in PalTypes.h
	enum DMContentType
	{
		DMContentTypePrimary = 0,
		DMContentTypeTopLeftHeader = 1,
		DMContentTypeTopHeader = 2,
		DMContentTypeLeftHeader = 3,
		DMContentTypeCustom = 4,
		DMContentTypeDescendant = 5
	};

	// Keep in sync with XDMOverpanMode in PalTypes.h
	enum DMOverpanMode
	{
		DMOverpanModeDefault = 0x00,
		DMOverpanModeNone = 0x04
	};
}
