#nullable enable

using System;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using System.Threading;
using System.Globalization;
using Windows.ApplicationModel.Core;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml
{
	public partial class Application
	{
		partial void InitializePartial()
		{
			InitializeSystemTheme();
		}

		private static partial Application StartPartial(Func<ApplicationInitializationCallbackParams, Application> callback)
		{
			throw new NotSupportedException("Reference implementation");
		}
	}
}
