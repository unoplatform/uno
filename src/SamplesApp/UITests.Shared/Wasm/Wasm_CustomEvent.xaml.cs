using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Newtonsoft.Json.Linq;
#if __WASM__
using Uno.Foundation;
using Uno.Extensions;
#endif

namespace UITests.Shared.Wasm
{
#if __WASM__
	[Sample("Wasm", nameof(Wasm_CustomEvent))]
#endif
	public sealed partial class Wasm_CustomEvent : Page
	{
		public Wasm_CustomEvent()
		{
			this.InitializeComponent();
		}

#if __WASM__
		private protected override void OnLoaded()
		{
			var genericId = genericEvent.HtmlId;
			var stringId = customEventString.HtmlId;
			var jsonId = customEventJson.HtmlId;

			var script = @"
(function(id1, id2, id3){
	const div1 = document.getElementById(id1);
	const div2 = document.getElementById(id2);
	const div3 = document.getElementById(id3);

	const onDiv1 = function(){
		div1.dispatchEvent(new Event(""unoevent1""));
		div1.style.backgroundColor = ""pink"";
	};

	const onDiv2 = function(){
		div2.dispatchEvent(new CustomEvent(""unoevent2"", {detail:""String detail from event.""}));
		div2.style.backgroundColor = ""pink"";
	};

	const onDiv3 = function(){
		div3.dispatchEvent(new CustomEvent(""unoevent3"", {detail: {msg:""msg"",int:123,txt:""it works!""}}));
		div3.style.backgroundColor = ""pink"";
	};

	div1.addEventListener(""click"", onDiv1);
	div2.addEventListener(""click"", onDiv2);
	div3.addEventListener(""click"", onDiv3);

	return ""ok"";
})";
			WebAssemblyRuntime.InvokeJS($"{script}({genericId},{stringId},{jsonId});");

			genericEvent.RegisterHtmlEventHandler("unoevent1", OnUnoEvent1);
			genericEvent.RegisterHtmlEventHandler("unoevent1", OnUnoEvent1bis);
			customEventString.RegisterHtmlCustomEventHandler("unoevent2", OnUnoEvent2, isDetailJson: false);
			customEventString.RegisterHtmlCustomEventHandler("unoevent2", OnUnoEvent2bis, isDetailJson: false);
			customEventJson.RegisterHtmlCustomEventHandler("unoevent3", OnUnoEvent3, isDetailJson: true);
			customEventJson.RegisterHtmlCustomEventHandler("unoevent3", OnUnoEvent3bis, isDetailJson: true);
		}

		private void OnUnoEvent1(object sender, EventArgs e)
		{
			tapResult.Text = "[WORKING]";
			result.Text += $"Received generic event from {sender}\n.";
		}

		private void OnUnoEvent1bis(object sender, EventArgs e)
		{
			tapResult.Text = "Ok";
		}

		private void OnUnoEvent2(object sender, HtmlCustomEventArgs e)
		{
			tapResult.Text = "[WORKING]";
			result.Text += $"Received string event from {sender}: \"{e.Detail}\"\n.";
		}

		private void OnUnoEvent2bis(object sender, HtmlCustomEventArgs e)
		{
			tapResult.Text =
				e.Detail == "String detail from event."
					? "Ok"
					: "Error: received " + e.Detail;
		}

		private void OnUnoEvent3(object sender, HtmlCustomEventArgs e)
		{
			tapResult.Text = "[WORKING]";
			result.Text += $"Received json event from {sender}: {e.Detail}\n.";
		}

		private void OnUnoEvent3bis(object sender, HtmlCustomEventArgs e)
		{
			try
			{
				var json = JToken.Parse(e.Detail);
				if (json["msg"].Value<string>() == "msg" && json["int"].Value<int>() == 123 && json["txt"].Value<string>() == "it works!")
				{
					tapResult.Text = "Ok";
				}
				else
				{
					tapResult.Text = "Error: invalid json " + json.ToString(Newtonsoft.Json.Formatting.None);
				}

			}
			catch (Exception ex)
			{
				tapResult.Text = "Error: " + ex.Message;
			}
		}
#endif
	}
}
