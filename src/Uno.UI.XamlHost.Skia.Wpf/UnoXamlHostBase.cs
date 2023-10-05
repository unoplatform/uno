// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/CommunityToolkit/Microsoft.Toolkit.Win32/blob/master/Microsoft.Toolkit.Wpf.UI.XamlHost/WindowsXamlHost.cs

using System;
using Uno.UI.Runtime.Skia.Wpf;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Microsoft.UI.Xaml;
using WpfControl = global::System.Windows.Controls.Control;
using WUX = Microsoft.UI.Xaml;

namespace Uno.UI.XamlHost.Skia.Wpf
{
	/// <summary>
	/// UnoXamlHost control hosts UWP XAML content inside the Windows Presentation Foundation
	/// </summary>
	public abstract partial class UnoXamlHostBase : WpfControl, IWpfApplicationHost
	{
		/// <summary>
		/// An instance of <seealso cref="IXamlMetadataContainer"/>. Required to
		/// probe at runtime for custom UWP XAML type information.
		/// This must be implemented by the instance of <seealso cref="WUX.Application"/>
		/// </summary>
		/// <remarks>
		/// <seealso cref="WUX.Application"/> object is required for loading custom control metadata.  If a custom
		/// Application object is not provided by the application, the host control will create an instance of <seealso cref="XamlApplication"/>.
		/// Instantiation of the application object must occur before creating the DesktopWindowXamlSource instance.
		/// If no Application object is created before DesktopWindowXamlSource is created, DestkopWindowXamlSource
		/// will create an instance of <seealso cref="XamlApplication"/> that implements <seealso cref="IXamlMetadataContainer"/>.
		/// </remarks>
		private static readonly IXamlMetadataContainer _metadataContainer;

		static UnoXamlHostBase()
		{
			//TODO: These lines should be set in a different location, possibly in a more general way (for multi-window support) https://github.com/unoplatform/uno/issues/8978
			Windows.UI.Core.CoreDispatcher.DispatchOverride = d =>
			{
				if (global::System.Windows.Application.Current is { } app)
				{
					app.Dispatcher.BeginInvoke(d);
				}
				else
				{
					// The Application instance may not yet have been initialized, or may have already been disposed.
				}
			};

			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () =>
			{
				if (global::System.Windows.Application.Current is { } app)
				{
					return app.Dispatcher.CheckAccess();
				}
				else
				{
					// The Application instance may not yet have been initialized, or may have already been disposed.
					return false;
				}
			};

			if (MetadataProviderDiscovery.MetadataProviderFactory is null)
			{
				MetadataProviderDiscovery.MetadataProviderFactory = type =>
				{
					if (typeof(WUX.Application).IsAssignableFrom(type))
					{
						WUX.Application application = null;

						WUX.Application.Start(_ =>
						{
							application = (WUX.Application)Activator.CreateInstance(type);
						});

						return (WUX.Markup.IXamlMetadataProvider)application;
					}

					return null;
				};
			}

			_metadataContainer = XamlApplicationExtensions.GetOrCreateXamlMetadataContainer();
		}

		/// <summary>
		/// UWP XAML DesktopWindowXamlSource instance that hosts XAML content in a win32 application
		/// </summary>
		private protected readonly WUX.Hosting.DesktopWindowXamlSource _xamlSource;

		/// <summary>
		/// Private field that backs ChildInternal property.
		/// </summary>
		private WUX.UIElement _childInternal;

		/// <summary>
		///     Fired when UnoXamlHost root UWP XAML content has been updated
		/// </summary>
		public event EventHandler ChildChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="UnoXamlHostBase"/> class.
		/// </summary>
		/// <remarks>
		/// Default constructor is required for use in WPF markup. When the default constructor is called,
		/// object properties have not been set. Put WPF logic in OnInitialized.
		/// </remarks>
		public UnoXamlHostBase()
		{
			if (_metadataContainer is null)
			{
				throw new InvalidOperationException("Metadata container failed to initialize");
			}

			// Create DesktopWindowXamlSource, host for UWP XAML content
			_xamlSource = new WUX.Hosting.DesktopWindowXamlSource();

			// Hook DesktopWindowXamlSource OnTakeFocus event for Focus processing
			_xamlSource.TakeFocusRequested += OnTakeFocusRequested;

			SizeChanged += OnSizeChanged;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UnoXamlHostBase"/> class.
		/// </summary>
		/// <remarks>
		/// Constructor is required for use in WPF markup. When the default constructor is called,
		/// object properties have not been set. Put WPF logic in OnInitialized.
		/// </remarks>
		/// <param name="typeName">UWP XAML Type name</param>
		public UnoXamlHostBase(string typeName)
			: this()
		{
			ChildInternal = UnoTypeFactory.CreateXamlContentByType(typeName);
			ChildInternal.SetWrapper(this);
		}

		// Uno specific: We need native overlay layer to show input for TextBoxes.
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_nativeOverlayLayer = GetTemplateChild("NativeOverlayLayer") as System.Windows.Controls.Canvas;
		}

		/// <summary>
		/// Gets the current instance of <seealso cref="XamlApplication"/>
		/// </summary>
		protected static IXamlMetadataContainer MetadataContainer
		{
			get
			{
				return _metadataContainer;
			}
		}

		/// <summary>
		/// Binds this wrapper object's exposed WPF DependencyProperty with the wrapped UWP object's DependencyProperty
		/// for what effectively works as a regular one- or two-way binding.
		/// </summary>
		/// <param name="propertyName">the registered name of the dependency property</param>
		/// <param name="wpfProperty">the DependencyProperty of the wrapper</param>
		/// <param name="uwpProperty">the related DependencyProperty of the UWP control</param>
		/// <param name="converter">a converter, if one's needed</param>
		/// <param name="direction">indicates that the binding should be one or two directional.  If one way, the Uwp control is only updated from the wrapper.</param>
		public void Bind(string propertyName, System.Windows.DependencyProperty wpfProperty, WUX.DependencyProperty uwpProperty, object converter = null, System.ComponentModel.BindingDirection direction = System.ComponentModel.BindingDirection.TwoWay)
		{
			if (direction == System.ComponentModel.BindingDirection.TwoWay)
			{
				var binder = new WUX.Data.Binding()
				{
					Source = this,
					Path = new WUX.PropertyPath(propertyName),
					Converter = (WUX.Data.IValueConverter)converter
				};
				WUX.Data.BindingOperations.SetBinding(ChildInternal, uwpProperty, binder);
			}

			var rebinder = new System.Windows.Data.Binding()
			{
				Source = ChildInternal,
				Path = new System.Windows.PropertyPath(propertyName),
				Converter = (System.Windows.Data.IValueConverter)converter
			};
			System.Windows.Data.BindingOperations.SetBinding(this, wpfProperty, rebinder);
		}

		/// <inheritdoc />
		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			InitializeHost();

			if (_childInternal != null)
			{
				SetContent();
			}
		}

		/// <summary>
		/// Gets or sets the root UWP XAML element displayed in the WPF control instance.
		/// </summary>
		/// <value>The <see cref="WUX.UIElement"/> child.</value>
		/// <remarks>This UWP XAML element is the root element of the wrapped <see cref="WUX.Hosting.DesktopWindowXamlSource" />.</remarks>
		protected WUX.UIElement ChildInternal
		{
			get
			{
				return _childInternal;
			}
			set
			{
				if (value == ChildInternal)
				{
					return;
				}

				var currentRoot = (WUX.FrameworkElement)ChildInternal;
				if (currentRoot != null)
				{
					currentRoot.SizeChanged -= XamlContentSizeChanged;
					WpfManager.XamlRootMap.Unregister(currentRoot.XamlRoot);
				}

				_childInternal = value;

				if (_childInternal.XamlRoot is not null)
				{
					WpfManager.XamlRootMap.Register(_childInternal.XamlRoot, this);
				}
				else if (_childInternal is FrameworkElement element)
				{
					element.Loading += OnChildLoading;
				}

				SetContent();

				var frameworkElement = ChildInternal as WUX.FrameworkElement;
				if (frameworkElement is not null)
				{
					// If XAML content has changed, check XAML size
					// to determine if UnoXamlHost needs to re-run layout.
					frameworkElement.SizeChanged += XamlContentSizeChanged;
					WpfManager.XamlRootMap.Register(frameworkElement.XamlRoot, this);
				}

				OnChildChanged();

				// Fire updated event
				ChildChanged?.Invoke(this, new EventArgs());

				UpdateUnoSize();
			}
		}

		private void OnChildLoading(FrameworkElement sender, object args)
		{
			// Ensure the XamlRoot is registered early.
			WpfManager.XamlRootMap.Register(sender.XamlRoot, this);
		}

		/// <summary>
		/// Called when the property <seealso cref="ChildInternal"/> has changed.
		/// </summary>
		protected virtual void OnChildChanged()
		{
		}

		/// <summary>
		/// Exposes ChildInternal without exposing its actual Type.
		/// </summary>
		/// <returns>the underlying UWP child object</returns>
		public object GetUwpInternalObject()
		{
			return ChildInternal;
		}

		/// <summary>
		/// Gets a value indicating whether this wrapper control instance been disposed
		/// </summary>
		public bool IsDisposed { get; private set; }

		private System.Windows.Window _parentWindow;

		//     /// <summary>
		//     /// Creates <see cref="WUX.Application" /> object, wrapped <see cref="WUX.Hosting.DesktopWindowXamlSource" /> instance; creates and
		//     /// sets root UWP XAML element on <see cref="WUX.Hosting.DesktopWindowXamlSource" />.
		//     /// </summary>
		//     /// <param name="hwndParent">Parent window handle</param>
		//     /// <returns>Handle to XAML window</returns>
		//     protected override HandleRef BuildWindowCore(HandleRef hwndParent)
		//     {
		//return default;
		//         //this._parentWindow = System.Windows.Window.GetWindow(this);
		//         //if (_parentWindow != null)
		//         //{
		//         //    _parentWindow.Closed += this.OnParentClosed;
		//         //}

		//         //ComponentDispatcher.ThreadFilterMessage += this.OnThreadFilterMessage;

		//         //// 'EnableMouseInPointer' is called by the WindowsXamlManager during initialization. No need
		//         //// to call it directly here.

		//         //// Create DesktopWindowXamlSource instance
		//         //var desktopWindowXamlSourceNative = _xamlSource.GetInterop<IDesktopWindowXamlSourceNative>();

		//         //// Associate the window where UWP XAML will display content
		//         //desktopWindowXamlSourceNative.AttachToWindow(hwndParent.Handle);

		//         //var windowHandle = desktopWindowXamlSourceNative.WindowHandle;

		//         //// Overridden function must return window handle of new target window (DesktopWindowXamlSource's Window)
		//         //return new HandleRef(this, windowHandle);
		//     }

		/// <summary>
		/// The default implementation of SetContent applies ChildInternal to desktopWindowXamSource.Content.
		/// Override this method if that shouldn't be the case.
		/// For example, override if your control should be a child of another UnoXamlHostBase-based control.
		/// </summary>
		protected virtual void SetContent()
		{
			if (_xamlSource != null)
			{
				_xamlSource.Content = _childInternal;
				_xamlSource.XamlIsland.IsSiteVisible = true;
				TryLoadContent();
			}
		}


		protected void TryLoadContent()
		{
			if (IsLoaded && _childInternal.XamlRoot is not null)
			{
				ContentManager.TryLoadRootVisual(_xamlSource.XamlIsland.XamlRoot);
			}
		}

		/// <summary>
		/// Disposes the current instance in response to the parent window getting destroyed.
		/// </summary>
		/// <param name="sender">Paramter sender is ignored</param>
		/// <param name="e">Parameter args is ignored</param>
		private void OnParentClosed(object sender, EventArgs e)
		{
			//this.Dispose(true);
		}

		///// <summary>
		///// WPF framework request to destroy control window.  Cleans up the HwndIslandSite created by DesktopWindowXamlSource
		///// </summary>
		///// <param name="hwnd">Handle of window to be destroyed</param>
		//protected override void DestroyWindowCore(HandleRef hwnd)
		//{
		//    Dispose(true);
		//}

		/// <summary>
		/// UnoXamlHost Dispose
		/// </summary>
		/// <param name="disposing">Is disposing?</param>
		protected void Dispose()
		{
			if (!this.IsDisposed)
			{
				var currentRoot = (WUX.FrameworkElement)ChildInternal;
				if (currentRoot != null)
				{
					currentRoot.SizeChanged -= XamlContentSizeChanged;
				}

				// Free any other managed objects here.
				//ComponentDispatcher.ThreadFilterMessage -= this.OnThreadFilterMessage;
				ChildInternal = null;
				if (_xamlSource != null)
				{
					_xamlSource.TakeFocusRequested -= OnTakeFocusRequested;
				}

				if (_parentWindow != null)
				{
					_parentWindow.Closed -= this.OnParentClosed;
					_parentWindow = null;
				}
			}

			// Free any unmanaged objects here.
			if (_xamlSource != null && !this.IsDisposed)
			{
				_xamlSource.Dispose();
			}

			// BUGBUG: CoreInputSink cleanup is failing when explicitly disposing
			// WindowsXamlManager.  Add dispose call back when that bug is fixed in 19h1.
			this.IsDisposed = true;

			// Call base class implementation.
			//base.Dispose(disposing);
		}

		//protected override System.IntPtr WndProc(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam, ref bool handled)
		//{
		//    const int WM_GETOBJECT = 0x003D;
		//    switch (msg)
		//    {
		//        // We don't want HwndHost to handle the WM_GETOBJECT.
		//        // Instead we want to let the HwndIslandSite's WndProc get it
		//        // So return handled = false and don't let the base class do
		//        // anything on that message.
		//        case WM_GETOBJECT:
		//            handled = false;
		//            return System.IntPtr.Zero;
		//    }

		//    return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
		//}
	}
}
