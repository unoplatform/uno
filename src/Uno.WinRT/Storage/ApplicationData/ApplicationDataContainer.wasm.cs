#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Interop;
using Uno.Foundation.Logging;
using Windows.Foundation.Collections;

namespace Windows.Storage;

public partial class ApplicationDataContainer
{
	partial void InitializePartial(ApplicationData owner)
	{
		Values = new FilePropertySet(owner, Locality);
	}
}
