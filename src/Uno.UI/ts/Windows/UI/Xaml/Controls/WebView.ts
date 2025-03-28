namespace Windows.UI.Xaml.Controls {

	export class WebView {
		static reload(htmlId: string): void {
			(<HTMLIFrameElement>document.getElementById(htmlId)).contentWindow.location.reload();
		}

		static stop(htmlId: string): void {
			(<HTMLIFrameElement>document.getElementById(htmlId)).contentWindow.stop();
		}

		static goBack(htmlId: string): void {
			(<HTMLIFrameElement>document.getElementById(htmlId)).contentWindow.history.back();
		}

		static goForward(htmlId: string): void {
			(<HTMLIFrameElement>document.getElementById(htmlId)).contentWindow.history.forward();
		}

		static executeScript(htmlId: string, script: string): string {
			return ((<HTMLIFrameElement>document.getElementById(htmlId)).contentWindow as any).eval(script);
		}

		static getDocumentTitle(htmlId: string): string {
			return (<HTMLIFrameElement>document.getElementById(htmlId)).contentDocument.title;
		}
	}
}
