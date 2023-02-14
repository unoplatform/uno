using System;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia
{
	internal class ApplicationExtension : IApplicationExtension
	{
		private readonly Application _owner;

		public ApplicationExtension(Application owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		}

#pragma warning disable CS0067 // The event is never used
		public event EventHandler? SystemThemeChanged;
#pragma warning restore CS0067 // The event is never used		
	}
}
