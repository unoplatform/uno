// Imported from https://github.com/VsixCommunity/Community.VisualStudio.Toolkit
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Uno.UI.RemoteControl.VS.Notifications;

/// <summary>
/// Creates InfoBar controls for use on documents and tool windows.
/// </summary>
public class InfoBarFactory
{
	private IVsInfoBarUIFactory _infoBarUIFactory;
	private readonly IVsShell _shell;
	private List<InfoBar> _infoBars = new();

	public InfoBarFactory(IVsInfoBarUIFactory infoBarUIFactory, IVsShell shell)
	{
		_infoBarUIFactory = infoBarUIFactory;
		_shell = shell;
	}

	/// <summary>
	/// Creates a new InfoBar in the main window.
	/// </summary>
	/// <param name="model">A model representing the text, icon, and actions of the InfoBar.</param>
	public async Task<InfoBar?> CreateAsync(InfoBarModel model)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		_shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var value);

		if (value is IVsInfoBarHost host)
		{
			var infoBar = new InfoBar(this, host, _infoBarUIFactory, model);
			_infoBars.Add(infoBar);
			return infoBar;
		}

		return null;
	}

	internal void RemoveInfoBar(InfoBar infoBar)
	{
		_infoBars.Remove(infoBar);
	}

	internal void RemoveAllInfoBars()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		foreach (var infoBar in _infoBars.ToArray())
		{
			infoBar.Close();
		}
	}

	internal void Dispose()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		RemoveAllInfoBars();
	}
}

/// <summary>
/// An instance of an InfoBar (also known as Yellow- or Gold bar).
/// </summary>
public class InfoBar : IVsInfoBarUIEvents
{
	private readonly IVsInfoBarHost _host;
	private readonly IVsInfoBarUIFactory _infoBarUIFactory;
	private readonly InfoBarModel _model;
	private IVsInfoBarUIElement? _uiElement;
	private uint _listenerCookie;
	private InfoBarFactory _infoBarFactory;

	/// <summary>
	/// Creates a new instance of the InfoBar in a specific window frame or document window.
	/// </summary>
	internal InfoBar(InfoBarFactory infoBarFactory, IVsInfoBarHost host, IVsInfoBarUIFactory infoBarUIFactory, InfoBarModel model)
	{
		_infoBarFactory = infoBarFactory;
		_host = host;
		_infoBarUIFactory = infoBarUIFactory;
		_model = model;
	}

	/// <summary>
	/// Indicates if the InfoBar is visible in the UI or not.
	/// </summary>
	public bool IsVisible { get; set; }

	/// <summary>
	/// Displays the InfoBar in the tool window or document previously specified.
	/// </summary>
	/// <returns><see langword="true" /> if the InfoBar was shown; otherwise <see langword="false" />.</returns>
	public async Task<bool> TryShowInfoBarUIAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		_uiElement = _infoBarUIFactory.CreateInfoBar(_model);
		_uiElement.Advise(this, out _listenerCookie);

		if (_host != null)
		{
			_host.AddInfoBar(_uiElement);
			IsVisible = true;
		}

		return IsVisible;
	}

	/// <summary>
	/// Closes the InfoBar without the user manually had to do it.
	/// </summary>
	public void Close()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (IsVisible && _uiElement != null)
		{
			_uiElement.Close();
		}
	}

	/// <summary>
	/// An event triggered when an action item in the InfoBar is clicked.
	/// </summary>
	public event EventHandler<InfoBarActionItemEventArgs>? ActionItemClicked;

	void IVsInfoBarUIEvents.OnClosed(IVsInfoBarUIElement infoBarUIElement)
	{
		IsVisible = false;
		ThreadHelper.ThrowIfNotOnUIThread();
		_uiElement?.Unadvise(_listenerCookie);
		_infoBarFactory.RemoveInfoBar(this);
	}

	void IVsInfoBarUIEvents.OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
	{
		ActionItemClicked?.Invoke(this, new InfoBarActionItemEventArgs(infoBarUIElement, _model, actionItem));
	}
}

record class ActionBarTextSpan(
	string Text,
	bool Bold = false,
	bool Italic = false,
	bool Underline = false) : IVsInfoBarTextSpan;

record ActionBarItem(
	string? Text,
	string? Name = null,
	bool Bold = false,
	bool Italic = false,
	bool Underline = false,
	object? ActionContext = null,
	bool IsButton = false) : IVsInfoBarActionItem;
