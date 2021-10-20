using System;
using Windows.UI.ViewManagement;
using Windows.Foundation;

namespace Windows.UI.ViewManagement
{
    /// <summary>
    /// Exposes Jetpack Window Manager fold information (Bounds, OcclusionType, State, Orientation)
    /// </summary>
    /// <remarks>
    /// https://docs.microsoft.com/dual-screen/android/jetpack/window-manager/
    /// https://developer.android.com/jetpack/androidx/releases/window
    /// TODO: consider correctly modelling a collection of FoldingFeatures for possible future multi-fold devices
    /// </remarks>
    public interface INativeFoldableProvider
    {
        /// <summary>
        /// Bounds of the hinge/fold area
        /// </summary>
        Rect Bounds { get; }

        // HACK: Choosing `bool` to hide custom types in Xamarin.AndroidX.Window
        // currently both these properties only have two values, although
        // this is not properly modelling them and is not future-proof

        /// <summary>
        /// true=FoldingFeatureOcclusionType.Full
        /// false=FoldingFeatureOcclusionType.None
        /// </summary>
        bool IsOccluding { get; }
        /// <summary>
        /// true=FoldingFeatureState.Flat
        /// false=FoldingFeatureState.HalfOpened
        /// </summary>
        bool IsFlat { get; }
        /// <summary>
        /// true=FoldingFeatureOrientation.Vertical
        /// false=FoldingFeatureOrientation.Horizontal
        /// </summary>
        bool IsVertical { get; }

        
    }
}
