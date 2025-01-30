using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft/* UWP don't rename */.UI.Xaml;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;
using Microsoft/* UWP don't rename */.UI.Xaml.Data;
using Microsoft/* UWP don't rename */.UI.Xaml.Input;
using Microsoft/* UWP don't rename */.UI.Xaml.Media;
using Microsoft/* UWP don't rename */.UI.Xaml.Navigation;
using Microsoft/* UWP don't rename */.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ResourcesExtractor
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft/* UWP don't rename */.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        private Window m_window;
    }
}
