using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		public UIElement()
		{
			_isFrameworkElement = this is FrameworkElement; // Avoids unused field error
			Initialize();
			InitializePointers();
		}

		// Used only on Skia. It's here and not in UIElement.skia.cs because it's accessed by SKCanvasElement which
		// builds against the reference API
		private protected virtual ContainerVisual CreateElementVisual() => throw new NotSupportedException("Reference assembly");

		public IntPtr Handle { get; }

		internal bool IsMeasureDirtyPath => throw new NotSupportedException("Reference assembly");

		internal bool IsArrangeDirtyPath => throw new NotSupportedException("Reference assembly");

		internal bool ShouldInterceptInvalidate { get; set; }

		internal void AddChild(UIElement child, int? index = null) => throw new NotSupportedException("Reference assembly");

		internal void MoveChildTo(int oldIndex, int newIndex) => throw new NotSupportedException("Reference assembly");

		internal bool RemoveChild(UIElement child) => throw new NotSupportedException("Reference assembly");

		internal UIElement ReplaceChild(int index, UIElement child) => throw new NotSupportedException("Reference assembly");

		internal void ClearChildren() => throw new NotSupportedException("Reference assembly");

		internal void UpdateHitTest() => throw new NotSupportedException("Reference assembly");
	}
}
