#if NET46 || __MACOS__
#pragma warning disable CS0067
#endif

using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.UI.Xaml.Controls.Primitives
{
	public sealed partial class Thumb : Control
	{
		public event DragCompletedEventHandler DragCompleted;
		public event DragDeltaEventHandler DragDelta;
		public event DragStartedEventHandler DragStarted;

		#region IsDragging DependencyProperty

		public bool IsDragging
		{
			get { return (bool)GetValue(IsDraggingProperty); }
			set { SetValue(IsDraggingProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsDragging.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsDraggingProperty =
			DependencyProperty.Register("IsDragging", typeof(bool), typeof(Thumb), new PropertyMetadata(false, (s, e) => ((Thumb)s)?.OnIsDraggingChanged(e)));

		private void OnIsDraggingChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		public Thumb()
		{
			// Call Initialise to allow platform-specific code execution 
			Initialize();
		}

		/// <summary>
		/// Initializes necessary platform-specific components
		/// </summary>
		partial void Initialize();

		public void CancelDrag() { }
	}
}
