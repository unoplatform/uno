#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
