using Uno.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Runtime.CompilerServices;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Diagnostics.Eventing;

namespace Windows.UI.Xaml
{
	public interface IDependencyObjectStoreProvider
	{
		DependencyObjectStore Store { get; }
	}
}
