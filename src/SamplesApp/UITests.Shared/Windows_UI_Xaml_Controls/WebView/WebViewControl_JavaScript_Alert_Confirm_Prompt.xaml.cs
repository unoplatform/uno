using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.WebView
{
	[Sample("WebView", "Javascript_AlertConfirmPrompt")]
	public sealed partial class WebViewControlJavaScriptAlertConfirmPrompt : UserControl
	{
#if HAS_UNO
		public WebViewControlJavaScriptAlertConfirmPrompt()
		{
			InitializeComponent();
			Loaded += WebViewControlWithJavaError_Loaded;
		}

		private void WebViewControlWithJavaError_Loaded(object sender, RoutedEventArgs e)
		{
			var sampleHTML = @"
<html>
	<head>
		<title></title>
	</head>
	<body>
	<p><br />This is a WKWebView</p>
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
			MyWebView.NavigateToString(sampleHTML);
		}
#endif
	}
}
