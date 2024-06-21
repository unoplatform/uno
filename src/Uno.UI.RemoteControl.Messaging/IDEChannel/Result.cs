#nullable enable
using System;
using System.Linq;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public record Result(string? Error)
{
	public static Result Success() => new(default(string));

	public static Result Fail(Exception error) => new(error.ToString());

	public bool IsSuccess => Error is null;

	public string? Error { get; } = Error;
}
