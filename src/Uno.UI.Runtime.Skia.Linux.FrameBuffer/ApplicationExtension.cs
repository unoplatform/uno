using System;
using Uno.UI.Xaml;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia
{
	internal class ApplicationExtension : IApplicationExtension
	{
		private readonly Application _owner;

		public ApplicationExtension(Application owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		}

		public bool CanExit => true;

		/// <summary>
		/// Determines if <see cref="Application.Exit()"/> has been called.
		/// </summary>
		internal bool ShouldExit { get; private set; }

#pragma warning disable CS0067 // The event is never used
		public event EventHandler? SystemThemeChanged;
#pragma warning restore CS0067 // The event is never used

		internal event EventHandler? ExitRequested;

		public void Exit()
		{
			ShouldExit = true;
			ExitRequested?.Invoke(this, EventArgs.Empty);
		}
	}
}
