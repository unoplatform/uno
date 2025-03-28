#nullable enable

using System;
using System.Linq;

namespace Windows.UI.Xaml
{
	// Internal interface used to allow communication between the real FrameworkElement
	// and presenters that are only implementing the IFrameworkElement interface (cf. FrameworkElementMixins.tt).
	// It must not be used anywhere else out of the file.
	internal interface IFrameworkElement_EffectiveViewport
	{
		void InitializeEffectiveViewport();
		IDisposable RequestViewportUpdates(bool isInternal, IFrameworkElement_EffectiveViewport child);
		void OnParentViewportChanged(bool isInitial, bool isInternal, IFrameworkElement_EffectiveViewport parent, ViewportInfo viewport);

#if !UNO_REFERENCE_API // Only called by Layouter which is under !UNO_REFERENCE_API condition.
		void OnLayoutUpdated();
#endif
	}
}
