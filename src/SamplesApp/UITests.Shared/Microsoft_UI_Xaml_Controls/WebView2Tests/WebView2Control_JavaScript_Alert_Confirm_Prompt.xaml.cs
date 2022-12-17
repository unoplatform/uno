using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[SampleControlInfo("WebView", "Javascript_AlertConfirmPrompt")]
	public sealed partial class WebView2ControlJavaScriptAlertConfirmPrompt : UserControl
	{
#if HAS_UNO
		public WebView2ControlJavaScriptAlertConfirmPrompt()
		{
			InitializeComponent();
			Loaded += WebView2ControlWithJavaError_Loaded;
		}

		private void WebView2ControlWithJavaError_Loaded(object sender, RoutedEventArgs e)
		{
			var sampleHTML = @"
<html>
	<head>
		<title></title>
	</head>
	<body>
	<p><br />This is a WKWebView2</p>
	<p><br />Basic alert:</p>
	<button onclick='basicAlert()'>Tap Me</button>
	<p><br />Confirmation alert:</p>
	<button onclick='confirmationAlert()'>Tap Me</button>
	<p id='confirmResult'></p>
	<p><br />Prompt alert:</p>
	<button onclick='promptAlert()'>Tap Me</button>
	<p id='promptResult'></p>
	<script>
		function basicAlert() {
    		alert('Hello, World!');
		}

		function confirmationAlert() {
			var resultString;
    		var result = confirm('Are you sure?');
    		if (result == true) {
    			resultString = 'You tapped OK!';
    		} else {
    			resultString = 'You tapped Cancel!';
    		}
    		document.getElementById('confirmResult').innerHTML = resultString;
		}

		function promptAlert() {
			var result = prompt('Enter some text','Placeholder Text');
			if (result != null) {
				document.getElementById('promptResult').innerHTML = 'You wrote: ' + result;
			}
		}
	</script>
	</body>
</html>";
			MyWebView2.NavigateToString(sampleHTML);
		}
#endif
	}
}
