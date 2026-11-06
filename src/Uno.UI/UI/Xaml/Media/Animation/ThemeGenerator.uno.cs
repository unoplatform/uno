#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

// TA_TRANSFORM_TYPE (uxtheme.h)
internal enum ThemeAnimationTransformType
{
	Translate2D = 0,
	Scale2D = 1,
	Opacity = 2,
	Clip = 3,
}

// TA_TRANSFORM_FLAG (uxtheme.h)
[Flags]
internal enum ThemeAnimationTransformFlags
{
	None = 0,
	TargetValuesUser = 1,
	HasInitialValues = 2,
	HasOriginValues = 4,
}

// TA_TRANSFORM + TA_TRANSFORM_2D / TA_TRANSFORM_OPACITY (uxtheme.h)
internal sealed class ThemeAnimationTransform
{
	internal ThemeAnimationTransformType eTransformType { get; init; }
	internal uint dwTimingFunctionId { get; init; }
	internal uint dwStartTime { get; init; }
	internal uint dwDurationTime { get; init; }
	internal ThemeAnimationTransformFlags eFlags { get; init; }

	internal float rX { get; init; }
	internal float rY { get; init; }
	internal float rInitialX { get; init; }
	internal float rInitialY { get; init; }
	internal float rOriginX { get; init; }
	internal float rOriginY { get; init; }

	internal float rOpacity { get; init; }
	internal float rInitialOpacity { get; init; }
}

partial class ThemeGenerator
{
	// Uno-specific: the PVL definitions WinUI reads through uxtheme, dumped from Windows build 29680.
	// Only the storyboards the ListViewBaseItem chrome uses are present.
	private const uint TimingFunctionLinear = 1;
	private const uint TimingFunctionDecelerate = 4;
	private const uint TimingFunctionDeleteScale = 6;

	private static readonly Dictionary<uint, (Point cp2, Point cp3)> s_timingFunctions = new()
	{
		[TimingFunctionLinear] = (new Point(0, 0), new Point(1, 1)),
		[TimingFunctionDecelerate] = (new Point(0.1f, 0.9f), new Point(0.2f, 1f)),
		[TimingFunctionDeleteScale] = (new Point(0.11f, 0.5f), new Point(0.24f, 0.96f)),
	};

	private static readonly Dictionary<(int storyboardID, int targetID), ThemeAnimationTransform[]> s_transforms = new()
	{
		[(TAS_FADEIN, TA_FADEIN_SHOWN)] =
		[
			new() { eTransformType = ThemeAnimationTransformType.Opacity, dwTimingFunctionId = TimingFunctionLinear, dwDurationTime = 167, rOpacity = 1f },
		],
		[(TAS_FADEOUT, TA_FADEOUT_HIDDEN)] =
		[
			new() { eTransformType = ThemeAnimationTransformType.Opacity, dwTimingFunctionId = TimingFunctionLinear, dwDurationTime = 167, rOpacity = 0f },
		],
		[(TAS_DRAGSOURCESTART, TA_DRAGSOURCESTART_DRAGSOURCE)] =
		[
			new()
			{
				eTransformType = ThemeAnimationTransformType.Scale2D, dwTimingFunctionId = TimingFunctionDecelerate, dwDurationTime = 240,
				eFlags = ThemeAnimationTransformFlags.HasOriginValues, rX = 1.05f, rY = 1.05f, rOriginX = 0.5f, rOriginY = 0.5f,
			},
			new() { eTransformType = ThemeAnimationTransformType.Opacity, dwTimingFunctionId = TimingFunctionDecelerate, dwDurationTime = 240, rOpacity = 0.65f },
		],
		[(TAS_DRAGSOURCESTART, TA_DRAGSOURCESTART_AFFECTED)] =
		[
			new()
			{
				eTransformType = ThemeAnimationTransformType.Scale2D, dwTimingFunctionId = TimingFunctionDecelerate, dwDurationTime = 240,
				eFlags = ThemeAnimationTransformFlags.HasOriginValues, rX = 0.95f, rY = 0.95f, rOriginX = 0.5f, rOriginY = 0.5f,
			},
		],
		[(TAS_DRAGBETWEENENTER, TA_DRAGBETWEENENTER_AFFECTED)] =
		[
			new() { eTransformType = ThemeAnimationTransformType.Translate2D, dwTimingFunctionId = TimingFunctionDecelerate, dwDurationTime = 200, eFlags = ThemeAnimationTransformFlags.TargetValuesUser },
		],
		[(TAS_DRAGBETWEENLEAVE, TA_DRAGBETWEENLEAVE_AFFECTED)] =
		[
			new() { eTransformType = ThemeAnimationTransformType.Translate2D, dwTimingFunctionId = TimingFunctionDecelerate, dwDurationTime = 200, eFlags = ThemeAnimationTransformFlags.TargetValuesUser },
		],
		// Layout transitions, dumped from Windows build 29698.
		[(TAS_REPOSITION, TA_REPOSITION_TARGET)] =
		[
			new() { eTransformType = ThemeAnimationTransformType.Translate2D, dwTimingFunctionId = TimingFunctionDecelerate, dwDurationTime = 367, eFlags = ThemeAnimationTransformFlags.TargetValuesUser },
		],
		[(TAS_ADDTOGRID, TA_ADDTOGRID_ADDED)] =
		[
			new()
			{
				eTransformType = ThemeAnimationTransformType.Scale2D, dwTimingFunctionId = TimingFunctionDecelerate, dwStartTime = 166, dwDurationTime = 333,
				eFlags = ThemeAnimationTransformFlags.HasInitialValues | ThemeAnimationTransformFlags.HasOriginValues,
				rX = 1f, rY = 1f, rInitialX = 0.9f, rInitialY = 0.9f, rOriginX = 0.5f, rOriginY = 0.5f,
			},
			new()
			{
				eTransformType = ThemeAnimationTransformType.Opacity, dwTimingFunctionId = TimingFunctionLinear, dwStartTime = 166, dwDurationTime = 333,
				eFlags = ThemeAnimationTransformFlags.HasInitialValues, rOpacity = 1f, rInitialOpacity = 0f,
			},
		],
		[(TAS_ADDTOGRID, TA_ADDTOGRID_AFFECTED)] =
		[
			new() { eTransformType = ThemeAnimationTransformType.Translate2D, dwTimingFunctionId = TimingFunctionDecelerate, dwDurationTime = 400, eFlags = ThemeAnimationTransformFlags.TargetValuesUser },
		],
		[(TAS_DELETEFROMGRID, TA_DELETEFROMGRID_DELETED)] =
		[
			new()
			{
				eTransformType = ThemeAnimationTransformType.Scale2D, dwTimingFunctionId = TimingFunctionDeleteScale, dwDurationTime = 100,
				eFlags = ThemeAnimationTransformFlags.HasOriginValues, rX = 0.9f, rY = 0.9f, rOriginX = 0.5f, rOriginY = 0.5f,
			},
			new() { eTransformType = ThemeAnimationTransformType.Opacity, dwTimingFunctionId = TimingFunctionLinear, dwDurationTime = 100, rOpacity = 0f },
		],
		[(TAS_DELETEFROMGRID, TA_DELETEFROMGRID_REMAINING)] =
		[
			new() { eTransformType = ThemeAnimationTransformType.Translate2D, dwTimingFunctionId = TimingFunctionDecelerate, dwDurationTime = 333, eFlags = ThemeAnimationTransformFlags.TargetValuesUser },
		],
		[(TAS_TRANSITIONCONTENT, TA_TRANSITIONCONTENT_INCOMING)] =
		[
			new() { eTransformType = ThemeAnimationTransformType.Translate2D, dwTimingFunctionId = TimingFunctionDecelerate, dwDurationTime = 500, eFlags = ThemeAnimationTransformFlags.TargetValuesUser },
			new()
			{
				eTransformType = ThemeAnimationTransformType.Opacity, dwTimingFunctionId = TimingFunctionDecelerate, dwStartTime = 50, dwDurationTime = 170,
				eFlags = ThemeAnimationTransformFlags.HasInitialValues, rOpacity = 1f, rInitialOpacity = 0f,
			},
		],
		[(TAS_TRANSITIONCONTENT, TA_TRANSITIONCONTENT_OUTGOING)] =
		[
			new() { eTransformType = ThemeAnimationTransformType.Opacity, dwTimingFunctionId = TimingFunctionLinear, dwDurationTime = 50, rOpacity = 0f },
		],
		[(TAS_ENTERPAGE, TA_ENTERPAGE_TARGET)] =
		[
			new() { eTransformType = ThemeAnimationTransformType.Translate2D, dwTimingFunctionId = TimingFunctionDecelerate, dwDurationTime = 667, eFlags = ThemeAnimationTransformFlags.TargetValuesUser },
			new()
			{
				eTransformType = ThemeAnimationTransformType.Opacity, dwTimingFunctionId = TimingFunctionDecelerate, dwDurationTime = 667,
				eFlags = ThemeAnimationTransformFlags.HasInitialValues, rOpacity = 1f, rInitialOpacity = 0f,
			},
		],
	};
}

partial class ThemeGeneratorHelper
{
	private const string TransitionTargetPrefix = "(UIElement.TransitionTarget).";
	private const string CompositeTransformPrefix = "(TransitionTarget.CompositeTransform).";
	private const string ClipTransformPrefix = "(TransitionTarget.ClipTransform).";

	// Uno-specific: UIElement.TransitionTarget is not a DP, so "(UIElement.TransitionTarget)..." paths
	// are resolved here and the timeline targets the TransitionTarget layer by reference.
	private void SetTimelineTarget(Timeline timeline, string propertyName)
	{
		if (m_target is not null)
		{
			var (target, leafPropertyName) = ResolveTarget(m_target, propertyName);
			Storyboard.SetTarget(timeline, target);
			Storyboard.SetTargetProperty(timeline, leafPropertyName);
		}
		else if (!string.IsNullOrEmpty(m_targetName))
		{
			// TODO Uno: a named target cannot resolve "(UIElement.TransitionTarget)" paths.
			Storyboard.SetTargetName(timeline, m_targetName);
			Storyboard.SetTargetProperty(timeline, propertyName);
		}
		else
		{
			Storyboard.SetTargetProperty(timeline, propertyName);
		}
	}

	private static (DependencyObject target, string propertyName) ResolveTarget(DependencyObject target, string propertyName)
	{
		if (target is not UIElement element || !propertyName.StartsWith(TransitionTargetPrefix, StringComparison.Ordinal))
		{
			return (target, propertyName);
		}

		// WinUI creates the TransitionTarget lazily when the path is resolved.
		var transitionTarget = element.TransitionTarget ??= new TransitionTarget();
		var path = propertyName.Substring(TransitionTargetPrefix.Length);

		if (path.StartsWith(CompositeTransformPrefix, StringComparison.Ordinal))
		{
			return (transitionTarget.CompositeTransform!, StripOwner(path.Substring(CompositeTransformPrefix.Length)));
		}

		if (path.StartsWith(ClipTransformPrefix, StringComparison.Ordinal))
		{
			return (transitionTarget.ClipTransform!, StripOwner(path.Substring(ClipTransformPrefix.Length)));
		}

		return (transitionTarget, path);
	}

	// "(CompositeTransform.ScaleX)" -> "ScaleX"
	private static string StripOwner(string path) =>
		path.StartsWith('(') && path.EndsWith(')') ? path.Substring(path.IndexOf('.') + 1).TrimEnd(')') : path;
}
