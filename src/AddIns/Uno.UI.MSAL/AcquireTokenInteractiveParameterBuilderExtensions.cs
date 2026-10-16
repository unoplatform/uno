using System.Runtime.CompilerServices;
using Microsoft.Identity.Client;

namespace Uno.UI.MSAL
{
	public static class AcquireTokenInteractiveParameterBuilderExtensions
	{
		/// <summary>
		/// Add required helpers for the current Uno platform.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AcquireTokenInteractiveParameterBuilder WithUnoHelpers(this AcquireTokenInteractiveParameterBuilder builder)
		{
			return builder;
		}
	}
}
