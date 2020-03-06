using Android.App;
using Android.OS;
using Android.Views;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI;

namespace Windows.ApplicationModel.Core
{
	public partial class CoreApplicationViewTitleBar
	{
		public bool ExtendViewIntoTitleBar
		{
			get
			{
				if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
				{
					if (ContextHelper.Current is Activity activity)
					{
						return (activity.Window.Attributes.Flags & WindowManagerFlags.TranslucentStatus) != 0;
					}
				}

				return false;
			}
			set
			{
				if (ExtendViewIntoTitleBar != value)
				{
					if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
					{
						if (ContextHelper.Current is Activity activity)
						{
							if (value)
							{
								activity.Window.SetFlags(WindowManagerFlags.TranslucentStatus, WindowManagerFlags.TranslucentStatus);
							}
							else
							{
								activity.Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
							}

							ExtendViewIntoTitleBarChanged?.Invoke();
						}
						else
						{
							if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
							{
								this.Log().LogWarning($"Context helper is not yet available, cannot set TranslucentStatus");
							}
						}
					}
				}
			}
		}
	}
}
