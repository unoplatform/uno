using System;
using System.Threading.Tasks;
using Android.Content.Res;
using Android.OS;
using Uno.UI.Extensions;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Globalization;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI.ViewManagement;
using Colors = Microsoft.UI.Colors;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml;

public partial class Application
{
	partial void InitializePartial()
	{
		InitializeSystemTheme();
		PermissionsHelper.Initialize();
	}

	static partial Application StartPartial(Func<ApplicationInitializationCallbackParams, Application> callback)
	{
		return callback(new ApplicationInitializationCallbackParams());
	}

	/// <remarks>
	/// The 5 second timeout seems to be the safest timeout for suspension activities.
	/// See - https://stackoverflow.com/a/3987733/732221
	/// </remarks>
	private DateTimeOffset GetSuspendingOffset() => DateTimeOffset.Now.AddSeconds(5);
}
