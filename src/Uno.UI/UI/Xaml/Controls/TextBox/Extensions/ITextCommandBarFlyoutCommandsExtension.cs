#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls.Extensions;

/// <summary>
/// Lets a platform add its own text services to the touch selection flyout of a <see cref="TextBox"/>, the way
/// the native edit menus do (e.g. iOS Look Up, Search Web, Scan Text, Share). The commands go to the flyout's
/// overflow, after the standard WinUI commands.
/// </summary>
/// <remarks>Registered as a singleton through ApiExtensibility.</remarks>
internal interface ITextCommandBarFlyoutCommandsExtension
{
	/// <summary>
	/// Returns the commands that apply to <paramref name="target"/>'s current selection. Called every time the
	/// touch selection flyout opens, so it should be cheap.
	/// </summary>
	IEnumerable<TextFlyoutPlatformCommand> GetCommands(TextBox target);
}

/// <param name="Id">Stable identifier, used to reuse the flyout button across openings.</param>
/// <param name="Label">The (localized) label shown in the flyout.</param>
/// <param name="Execute">Runs the command. The flyout closes itself first.</param>
internal sealed record TextFlyoutPlatformCommand(string Id, string Label, Action Execute);
