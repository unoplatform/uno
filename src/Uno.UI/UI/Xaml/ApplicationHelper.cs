using System;
using Microsoft.UI.Xaml;

namespace Uno.UI
{
	public class ApplicationHelper
	{
		private static string _requestedCustomTheme;

		/// <summary>
		/// This is a custom theme that can be used in ThemeDictionaries
		/// </summary>
		/// <remarks>
		/// When the custom theme key is not found in a theme dictionary, it will fallback to
		/// Application.RequestedTheme (Dark/Light)
		/// </remarks>
		public static string RequestedCustomTheme
		{
			get => _requestedCustomTheme;
			set
			{
				_requestedCustomTheme = value;
				if (_requestedCustomTheme != null)
				{
					if (_requestedCustomTheme.Equals("Dark"))
					{
						Application.Current.RequestedTheme = ApplicationTheme.Dark;
					}
					else if (_requestedCustomTheme.Equals("Light"))
					{
						Application.Current.RequestedTheme = ApplicationTheme.Light;
					}
				}

				Application.UpdateRequestedThemesForResources();
			}
		}

		/// <summary>
		/// Force all {ThemeResource} declarations to reevaluate its bindings.
		/// </summary>
		/// <remarks>
		/// This could be useful if you manually changed the bound values in global
		/// themed dictionary and you want to reapply them without having to toggle
		/// dark/light and producing annoying flickering to user.
		/// 
		/// Only applications with dynamic color schemes should use this.
		/// </remarks>
		public static void ReapplyApplicationTheme()
			=> Application.Current.OnRequestedThemeChanged();

		public static bool IsLoadableComponent(Uri resource)
		{
			return Application.Current?.IsLoadableComponent(resource) ?? false;
		}
	}
}
