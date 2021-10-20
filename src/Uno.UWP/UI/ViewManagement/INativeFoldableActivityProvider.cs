using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.ViewManagement
{
    public interface INativeFoldableActivityProvider
    {
        bool HasFoldFeature { get; }
        bool IsSeparating { get; }
        bool IsFoldVertical { get; }
        Rect FoldBounds { get; }

        event EventHandler<NativeFold> LayoutChanged;
    }
}
