using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Windowing.Native;

internal interface INativeOverlappedPresenter
{
	void SetIsResizable(bool isResizable);

	void SetIsModal(bool isModal);

	void SetIsMinimizable(bool isMinimizable);

	void SetIsMaximizable(bool isMaximizable);

	void SetIsAlwaysOnTop(bool isAlwaysOnTop);

	OverlappedPresenterState State { get; }

	void Maximize();

	void Minimize(bool activateWindow);

	void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar);

	void Restore(bool activateWindow);

	void SetPreferredMinimumSize(int? preferredMinimumWidth, int? preferredMinimumHeight);

	void SetPreferredMaximumSize(int? preferredMaximumWidth, int? preferredMaximumHeight);
}
