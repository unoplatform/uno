using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

#if __ANDROID__
using Android.Views;
using Android.App;
#endif

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Controls.WebView;

[Sample("WebView", Name = "WebView_Input_Keyboard_Insets",
	IsManualTest = true,
	Description = "Android Only. Other platform you will see an empty page. \n" +
	"When focusing inputs, inset will be properly added preventing keyboard from covering the focused input.")]
public sealed partial class WebView_Input_Keyboard_Insets : Page
{
	public WebView_Input_Keyboard_Insets()
	{
		this.InitializeComponent();
		this.Content = new WebViewContainer();
	}
}

public partial class WebViewContainer : Control { }

#if __ANDROID__
public partial class WebViewContainer
{
	public WebViewContainer()
	{
		Initialize();
	}

	private void Initialize()
	{
		HandleKeyboard();

		var webView = new Android.Webkit.WebView(this.Context!)
		{
			Focusable = true,
			FocusableInTouchMode = true
		};

		webView.Settings.JavaScriptEnabled = true;
		webView.Settings.DomStorageEnabled = true;
		webView.Settings.JavaScriptCanOpenWindowsAutomatically = true;

		AddView(webView);

		webView.LoadDataWithBaseURL(
			baseUrl: null,
			data: html,
			mimeType: "text/html",
			encoding: "utf-8",
			historyUrl: null
		);

		webView.SetFocusable(ViewFocusability.Focusable);
	}

	private void HandleKeyboard()
	{
		if (this.Context is Activity activity)
		{
			activity.Window?.SetSoftInputMode(SoftInput.AdjustResize | SoftInput.StateHidden);
		}
		Unloaded += (s, e) =>
		{
			if (this.Context is Activity activity)
			{
				activity.Window?.SetSoftInputMode(SoftInput.AdjustNothing | SoftInput.StateHidden);

			}
		};
	}

	string html = @"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>ID Verification</title>
  <style>
    html, body {
      margin: 0;
      padding: 0;
      height: 100%;
      font-family: sans-serif;
      background-color: #f8f8f8;
    }

    .wrapper {
      min-height: 100%;
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 24px;
      box-sizing: border-box;
    }

    .container {
      width: 100%;
      max-width: 400px;
      background-color: white;
      padding: 24px;
      border-radius: 10px;
      box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);
    }

    .group {
      margin-bottom: 48px; /* spacing 3? ?? */
    }

    label {
      font-weight: bold;
      margin-bottom: 4px;
      display: block;
    }

    input {
      width: 100%;
      padding: 12px;
      font-size: 16px;
      margin-top: 35px;
      box-sizing: border-box;
    }

    button {
      padding: 12px;
      width: 100%;
      background-color: #4CAF50;
      color: white;
      font-size: 16px;
      border: none;
      border-radius: 6px;
      cursor: pointer;
    }

    .hidden {
      display: none;
    }
  </style>
</head>
<body>
  <div class='wrapper'>
    <div class='container'>
      <div class='group'>
        <label for='name'>Please enter your name</label>
        <input type='text' id='name' placeholder='Your name' />
      </div>

      <div class='group' id='confirm-group'>
        <button id='confirm'>Confirm</button>
      </div>

      <div class='group' id='birth-group'>
        <label for='birth'>Please enter your birthdate (YYMMDD)</label>
        <input type='text' id='birth' maxlength='6' placeholder='e.g. 900101' />
      </div>

      <div class='group' id='gender-group'>
        <label for='gender'>Please enter your gender</label>
        <input type='text' id='gender' maxlength='1' placeholder='e.g. 1' />
      </div>

      <div class='group' id='phone-group'>
        <label for='phone'>Please enter your mobile number</label>
        <input type='tel' id='phone' maxlength='11' placeholder='e.g. 01012345678' />
      </div>

      <div class='group' id='country-group'>
        <label for='country'>Please enter Country</label>
        <input type='text' id='country' placeholder='e.g. Canada' />
      </div>
      <div class='group' id='country-group'>
        <label for='city'>Please enter City</label>
        <input type='text' id='city' placeholder='e.g. Montreal' />
      </div>
    </div>
  </div>

  <script>
    const confirmBtn = document.getElementById('confirm');
    const confirmGroup = document.getElementById('confirm-group');
    const birthGroup = document.getElementById('birth-group');
    const genderGroup = document.getElementById('gender-group');
    const phoneGroup = document.getElementById('phone-group');
    const birthInput = document.getElementById('birth');
    const genderInput = document.getElementById('gender');

    confirmBtn.addEventListener('click', () => {
      confirmGroup.classList.add('hidden');
      birthGroup.classList.remove('hidden');
    });

    // Show gender input after birthdate is fully entered
    birthInput.addEventListener('input', () => {
      if (birthInput.value.length === 6) {
        genderGroup.classList.remove('hidden');
      } else {
        genderGroup.classList.add('hidden');
        phoneGroup.classList.add('hidden');
      }
    });

    // Show phone input only after both birth and gender are valid + Enter pressed
    genderInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter' &&
          birthInput.value.length === 6 &&
          genderInput.value.length >= 1) {
        phoneGroup.classList.remove('hidden');
        phoneGroup.scrollIntoView({ behavior: 'smooth' });
      }
    });
  </script>
</body>
</html>";

}
#endif
