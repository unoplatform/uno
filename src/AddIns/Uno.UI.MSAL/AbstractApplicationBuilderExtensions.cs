using System.Runtime.CompilerServices;
using Microsoft.Identity.Client;
using Uno.UI.MSAL.Extensibility;

namespace Uno.UI.MSAL
{
	public static class AbstractApplicationBuilderExtensions
	{
		/// <summary>
		/// Add required helpers for the current Uno platform.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T WithUnoHelpers<T>(this T builder)
			where T : AbstractApplicationBuilder<T>
		{
			return DefaultMsalExtension.Default.InitializeAbstractApplicationBuilder(builder);
		}
	}
}
