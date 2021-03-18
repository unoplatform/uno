#if !HAS_UNO_WINUI
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
    public partial class MediaPlayerElement
    {
		public MediaPlayerElement()
		{
			DefaultStyleKey = typeof(MediaPlayerElement);
		}
    }
}
#endif
