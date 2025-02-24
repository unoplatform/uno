using System;
using System.Linq;
using Uno.Disposables;
using static Uno.UI.FeatureConfiguration;


#if HAS_UNO_WINUI
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ScrollBar
{
	[ThreadStatic]
	private static Orientation? _fixedOrientation;

#if !__SKIA__
	private bool? _hasFixedVisualStates;
#else

	internal static IDisposable MaterializingFixed(Orientation orientation)
	{
		_fixedOrientation = orientation;
		return Disposable.Create(() => _fixedOrientation = null);
	}

	/// <summary>
	/// Indicates if this scrollbar supports to change its orientation once its template has been applied (cf. remarks).
	/// This is false by default (which means that the ScrollBar will support dynamic orientation changes).
	/// </summary>
	/// <remarks>
	/// This flag is for performance consideration, it allows ScrollBar to load only half of its template.
	/// It's used by core controls (e.g. ScrollViewer) where the ScrollBar's orientation will never change.
	/// It's required as, unlike UWP, a control which is Visibility = Collapsed will get its template applied anyway
	/// on mobile targets. This is not the case on Skia and WebAssembly.
	/// </remarks>
	internal bool IsFixedOrientation { get; set; }

	private static void DetachEvents(object snd, RoutedEventArgs args) // OnUnloaded
		=> (snd as ScrollBar)?.DetachEvents();

	internal bool HasFixedVisualStates()
	{
#if __SKIA__
		return false;
#else
		if (this.GetTemplateRoot() is not { } templateRoot)
		{
			return false;
		}

		if (_hasFixedVisualStates is null)
		{
			var groups = VisualStateManager.GetVisualStateGroups(templateRoot);
			if (groups.FirstOrDefault(g => g.Name == "CommonStates") is { } commonStates)
			{
				_hasFixedVisualStates = commonStates.States?.Any(s => s.Name == "Vertical_Normal") ?? false;
			}
			else
			{
				_hasFixedVisualStates = false;
			}
		}

		return _hasFixedVisualStates.Value;
#endif
	}

#if !UNO_HAS_ENHANCED_LIFECYCLE
	private static void OnLayoutUpdated(
		object pSender,
		object pArgs)
	{
		(pSender as ScrollBar)?.UpdateTrackLayout();
	}
#endif
}
