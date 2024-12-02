namespace Microsoft.UI.Xaml.Controls {

	export class WebView {
		private static unoExports: any;

		public static buildImports(assembly: string) {
			(<any>window.Module).getAssemblyExports(assembly)
				.then((e: any) => {
					WebView.unoExports = e.Microsoft.UI.Xaml.Controls.NativeWebView;
				});
		}

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

		static setAttribute(htmlId: string, name: string, value: string) {
			(<HTMLIFrameElement>document.getElementById(htmlId)).setAttribute(name, value);
		}

		static getAttribute(htmlId: string, name: string) : string {
			return (<HTMLIFrameElement>document.getElementById(htmlId)).getAttribute(name);
		}

		static setBackground(htmlId: string, color: string) {
			(<HTMLIFrameElement>document.getElementById(htmlId)).style.backgroundColor = color;
		}

		static setupEvents(htmlId: string) {
			(<HTMLIFrameElement>document.getElementById(htmlId)).onload = () => {
				WebView.unoExports.DispatchLoadEvent(htmlId);
			}
		}
	}
}
