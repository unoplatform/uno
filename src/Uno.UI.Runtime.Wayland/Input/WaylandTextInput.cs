#nullable enable

using System;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland;

// zwp_text_input_v3 as the IME extension uses it, so its state handling is testable without a compositor.
internal interface IWaylandTextInput
{
	event EventHandler<ZwpTextInputV3.EnterEventArgs>? Enter;
	event EventHandler<ZwpTextInputV3.LeaveEventArgs>? Leave;
	event EventHandler<ZwpTextInputV3.PreeditStringEventArgs>? PreeditString;
	event EventHandler<ZwpTextInputV3.CommitStringEventArgs>? CommitString;
	event EventHandler<ZwpTextInputV3.DeleteSurroundingTextEventArgs>? DeleteSurroundingText;
	event EventHandler<ZwpTextInputV3.DoneEventArgs>? Done;
	void Enable();
	void Disable();
	void Commit();
	void SetTextChangeCause(uint cause);
	void SetContentType(uint hint, uint purpose);
	void SetSurroundingText(string text, int cursor, int anchor);
	void SetCursorRectangle(int x, int y, int width, int height);
}

internal sealed class WaylandTextInput(ZwpTextInputV3 input) : IWaylandTextInput
{
	public event EventHandler<ZwpTextInputV3.EnterEventArgs>? Enter
	{
		add => input.Enter += value;
		remove => input.Enter -= value;
	}

	public event EventHandler<ZwpTextInputV3.LeaveEventArgs>? Leave
	{
		add => input.Leave += value;
		remove => input.Leave -= value;
	}

	public event EventHandler<ZwpTextInputV3.PreeditStringEventArgs>? PreeditString
	{
		add => input.PreeditString += value;
		remove => input.PreeditString -= value;
	}

	public event EventHandler<ZwpTextInputV3.CommitStringEventArgs>? CommitString
	{
		add => input.CommitString += value;
		remove => input.CommitString -= value;
	}

	public event EventHandler<ZwpTextInputV3.DeleteSurroundingTextEventArgs>? DeleteSurroundingText
	{
		add => input.DeleteSurroundingText += value;
		remove => input.DeleteSurroundingText -= value;
	}

	public event EventHandler<ZwpTextInputV3.DoneEventArgs>? Done
	{
		add => input.Done += value;
		remove => input.Done -= value;
	}

	public void Enable() => input.Enable();
	public void Disable() => input.Disable();
	public void Commit() => input.Commit();
	public void SetTextChangeCause(uint cause) => input.SetTextChangeCause(cause);
	public void SetContentType(uint hint, uint purpose) => input.SetContentType(hint, purpose);
	public void SetSurroundingText(string text, int cursor, int anchor) => input.SetSurroundingText(text, cursor, anchor);
	public void SetCursorRectangle(int x, int y, int width, int height) => input.SetCursorRectangle(x, y, width, height);
}
