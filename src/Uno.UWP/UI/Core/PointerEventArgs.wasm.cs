using System.Collections.Generic;
using Windows.System;
using Windows.UI.Input;

namespace Windows.UI.Core;

partial class PointerEventArgs
{
	internal HtmlEventDispatchResult DispatchResult { get; set; }
}
