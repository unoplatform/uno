namespace Microsoft.UI.Xaml.Controls {

    export class WebView {
        private static unoExports: any;
        private static cachedPackageBase: string | null = null;

        public static buildImports(assembly: string) {
            if (!WebView.unoExports) {
                (<any>window.Module).getAssemblyExports(assembly)
                    .then((e: any) => {
                        WebView.unoExports = e.Microsoft.UI.Xaml.Controls.NativeWebView;
                    });
            }
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

        static getAttribute(htmlId: string, name: string): string {
            return (<HTMLIFrameElement>document.getElementById(htmlId)).getAttribute(name);
        }

        static navigate(htmlId: string, url: string) {
            const iframe = document.getElementById(htmlId) as HTMLIFrameElement;
            if (iframe?.contentWindow) {
                iframe.contentWindow.location.href = url;
            } else if (iframe) {
                iframe.setAttribute("src", url);
            }
        }

        static initializeStyling(htmlId: string) {
            const iframe = document.getElementById(htmlId) as HTMLIFrameElement;
            iframe.style.backgroundColor = "transparent";
            iframe.style.border = "0";
        }

        static getPackageBase(): string {
            if (WebView.cachedPackageBase !== null) {
                return WebView.cachedPackageBase;
            }

            const pathsToCheck = [
                ...Array.from(document.getElementsByTagName('script')).map(s => s.src),
            ];

            for (const path of pathsToCheck) {
                const m = path?.match(/\/package_[^\/]+/);
                if (m) {
                    const packageBase = "./" + m[0].substring(1);
                    WebView.cachedPackageBase = packageBase;
                    return packageBase;
                }
            }

            WebView.cachedPackageBase = ".";
            return ".";
        }

        static setupEvents(htmlId: string) {
            const iframe = <HTMLIFrameElement>document.getElementById(htmlId);
            iframe.addEventListener('load', WebView.onLoad);
        }

        static cleanupEvents(htmlId: string) {
            const iframe = <HTMLIFrameElement>document.getElementById(htmlId);
            iframe.removeEventListener('load', WebView.onLoad);
        }

        private static onLoad(event: Event) {
            const iframe = event.currentTarget as HTMLIFrameElement;
            const absoluteUrl = iframe.contentWindow.location.href;
            WebView.unoExports.DispatchLoadEvent(iframe.id, absoluteUrl);

            try {
                if (iframe.contentWindow && WebView.unoExports.DispatchNewWindowRequested) {
                    
                    const unoExports = WebView.unoExports;
                    
                    const originalOpen = iframe.contentWindow.open;
                    iframe.contentWindow.open = function(url, target, features) {
                        const referer = iframe.contentWindow.location.href;

                        const handled = unoExports.DispatchNewWindowRequested(
                            iframe.id,
                            url,
                            referer
                        );

                        if (!handled) {
                            return originalOpen.apply(this, arguments);
                        }
                        
                        return null;
                    };

                    const links = iframe.contentDocument.querySelectorAll('a[target="_blank"]');
                    links.forEach(link => {
                        link.addEventListener('click', (e: MouseEvent) => {
                            const targetUrl = (link as HTMLAnchorElement).href;
                            const referer = iframe.contentWindow.location.href;

                            const handled = unoExports.DispatchNewWindowRequested(
                                iframe.id,
                                targetUrl,
                                referer
                            );

                            if (handled) {
                                e.preventDefault();
                                e.stopPropagation();
                            }
                        });
                    });
                }
            } catch (e) {
                // This can fail if the iframe content is cross-origin.
                // We log this as a warning, as it's a known browser security feature.
                // https://developer.mozilla.org/en-US/docs/Web/Security/Same-origin_policy
                console.warn("Uno.WebView: Could not attach NewWindowRequested handlers. This is expected if the iframe content is cross-origin.");
            }
        }
    }
}
