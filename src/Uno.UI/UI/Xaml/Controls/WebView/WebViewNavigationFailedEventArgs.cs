#nullable disable

using Uno.UI.Web;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
    public partial class WebViewNavigationFailedEventArgs
    {
        public Uri Uri { get; internal set; }
        public WebErrorStatus WebErrorStatus { get; internal set; }
    }
}
