using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Windows.UI.ViewManagement
{
    public class NativeFold : INativeFoldableProvider
    {
        public Rect Bounds { get; set; }

        public bool IsOccluding { get; set; }

        public bool IsFlat { get; set; }

        public bool IsVertical { get; set; }
    }
}
