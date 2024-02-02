using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;
using Private.Infrastructure;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	internal class WebView2ViewModel : ViewModelBase
	{
		private const string SampleHtml = @"
<!DOCTYPE html>
<html>
	<head>
		<title>Uno Samples</title>
	</head>
	<body>
		<p>Welcome to <a href=""https://platform.uno/"">Uno Platform</a>'s samples!</p>
	</body>
</html>";

		public WebView2ViewModel(UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		public string InitialSourceString => SampleHtml;

		public string InitialUri => "http://www.google.com";

		public string AlertHtml => @"
<html>
	<head>
		<title>Spamming alert</title>
	</head>
	<body>
		<h1>This page spam alert each 5 seconds.</h1>
		<script>
		let count = 0;
		function timer() {
			count++;
			alert('Spamming alert #' + count);
			console.log(""Spamming alert #"" + count);
			setTimeout(timer, 5000)
		}
		timer()
		</script>
	</body>
</html>";
	}
}
