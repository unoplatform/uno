using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
#if __WASM__
using Uno.Foundation;
using Uno.Extensions;
#endif

namespace UITests.Shared.Wasm
{
#if __WASM__
	[SampleControlInfo("Wasm", nameof(Wasm_CustomEvent))]
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
	};

	const onDiv2 = function(){
		div2.dispatchEvent(new CustomEvent(""unoevent2"", {detail:""String detail from event.""}));
	};

	const onDiv3 = function(){
		div3.dispatchEvent(new CustomEvent(""unoevent3"", {detail: {msg:""msg"",int:123,txt:""it works!""}}));
	};

	div1.addEventListener(""click"", onDiv1);
	div2.addEventListener(""click"", onDiv2);
	div3.addEventListener(""click"", onDiv3);

	return ""ok"";
})";
			WebAssemblyRuntime.InvokeJS($"{script}({genericId},{stringId},{jsonId});");

			genericEvent.RegisterHtmlEventHandler("unoevent1", OnUnoEvent1);
			customEventString.RegisterHtmlCustomEventHandler("unoevent2", OnUnoEvent2, isDetailJson: false);
			customEventJson.RegisterHtmlCustomEventHandler("unoevent3", OnUnoEvent3, isDetailJson: true);

		}

		private void OnUnoEvent1(object sender, EventArgs e)
		{
			result.Text += $"Received generic event from {sender}\n.";
		}

		private void OnUnoEvent2(object sender, HtmlCustomEventArgs e)
		{
			result.Text += $"Received string event from {sender}: \"{e.Detail}\"\n.";
		}

		private void OnUnoEvent3(object sender, HtmlCustomEventArgs e)
		{
			result.Text += $"Received json event from {sender}: {e.Detail}\n.";
		}
#endif
	}
}
