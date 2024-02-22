#nullable enable

using System;
using Uno.Disposables;

namespace Microsoft.UI.Windowing;

public partial class AppWindowPresenter
{
	/// <summary>
	/// Displays an app window using a pre-defined configuration appropriate for the type of window.
	/// </summary>
	internal AppWindowPresenter(AppWindowPresenterKind kind) => Kind = kind;

	/// <summary>
	/// Gets a value that indicates the kind of presenter the app window is using.
	/// </summary>
	public AppWindowPresenterKind Kind { get; }

	internal AppWindow? Owner { get; private set; }

	internal void SetOwner(AppWindow? owner)
	{
		if (owner is not null && Owner is not null)
		{
			throw new InvalidOperationException("The presenter must first be unregistered from existing app window to be reusable.");
		}

		Owner = owner;

		// Cleanup old owner actions
		// Subscribe to new owner actions
	}
}
