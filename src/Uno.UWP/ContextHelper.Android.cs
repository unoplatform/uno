#nullable enable

using Uno.Foundation.Logging;

namespace Uno.UI;

public static class ContextHelper
{
	private static Android.App.Application? _application;
	private static Android.Content.Context? _current;

	/// <summary>
	/// Get the current android content context
	/// </summary>
	public static Android.Content.Context Current
	{
		get
		{
			if (_current is null)
			{
				typeof(ContextHelper)
					.Log()
					.Warn(
						"ContextHelper.Current not defined. " +
						"For compatibility with Uno, you should ensure your `MainActivity` " +
						"is deriving from Windows.UI.Xaml.ApplicationActivity.");
			}
			return _current!; // TODO:MZ: Nullable!
		}
		set => _current = value;
	}

	public static Android.App.Application? Application
	{
		get
		{
			if (_application is null)
			{
				typeof(ContextHelper)
					.Log()
					.Warn(
						"ContextHelper.ApplicationContext not defined. " +
						"For compatibility with Uno, you should ensure your Application " +
						"is deriving from Windows.UI.Xaml.NativeApplication.");
			}
			return _application;
		}
		internal set => _application = value;
	}

	/// <summary>
	/// Tries getting the current context.
	/// </summary>
	/// <param name="context">The context if available</param>
	/// <returns>true if the current context is available, otherwise false.</returns>
	internal static bool TryGetCurrent(out Android.Content.Context? context)
	{
		context = _current;
		return _current != null;
	}
}
