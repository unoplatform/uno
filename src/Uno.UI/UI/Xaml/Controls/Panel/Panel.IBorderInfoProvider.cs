#nullable enable

using System;
using Microsoft.UI.Composition;
using Uno.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class Panel : IBorderInfoProvider
{
	Brush? IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => InternalBackgroundSizing;

	Brush? IBorderInfoProvider.BorderBrush => BorderBrushInternal;

	Thickness IBorderInfoProvider.BorderThickness => BorderThicknessInternal;

	CornerRadius IBorderInfoProvider.CornerRadius => CornerRadiusInternal;

#if UNO_HAS_BORDER_VISUAL
	BorderVisual IBorderInfoProvider.BorderVisual => Visual as BorderVisual ?? throw new InvalidCastException($"{nameof(IBorderInfoProvider)}s should use a {nameof(BorderVisual)}.");
#endif
}
