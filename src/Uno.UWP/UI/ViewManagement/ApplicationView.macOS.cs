using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using AppKit;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using CoreGraphics;

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
		private string _title = string.Empty;
		private Size _preferredMinSize = Size.Empty;

		public string Title
		{
			get => IsKeyWindowInitialized() ? NSApplication.SharedApplication.KeyWindow.Title : _title;
			set
			{
				if (IsKeyWindowInitialized())
				{
					NSApplication.SharedApplication.KeyWindow.Title = value;
				}

				_title = value;
			}
		}

		public bool TryResizeView(Size value)
		{
			VerifyKeyWindowInitialized();

			if (value.Width < _preferredMinSize.Width || value.Height < _preferredMinSize.Height)
			{
				return false;
			}
			var window = NSApplication.SharedApplication.KeyWindow;
			var frame = window.Frame;
			frame.Size = value;
			window.SetFrame(frame, true, true);
			return true;
		}

		public void SetPreferredMinSize(Size minSize)
		{
			VerifyKeyWindowInitialized();

			var window = NSApplication.SharedApplication.KeyWindow;
			window.MinSize = new CGSize(minSize.Width, minSize.Height);
			_preferredMinSize = minSize;
		}

		internal void SyncTitleWithWindow(NSWindow window)
		{
			if (!string.IsNullOrWhiteSpace(_title))
			{
				window.Title = _title;
			}
		}

		private void VerifyKeyWindowInitialized([CallerMemberName] string propertyName = null)
		{
			if (!IsKeyWindowInitialized())
			{
				throw new InvalidOperationException($"{propertyName} API must be used after KeyWindow is set");
			}
		}

		private bool IsKeyWindowInitialized() => NSApplication.SharedApplication.KeyWindow != null;
	}
}
