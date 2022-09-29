#nullable disable

using System;

namespace Windows.UI.Core
{
	public interface ICoreWindowEvents
	{
		void RaiseKeyUp(KeyEventArgs args);
		void RaiseKeyDown(KeyEventArgs args);
	}
}

