namespace Microsoft.UI.Xaml.Controls {

    export class WebView {
        private static unoExports: any;
        private static cachedPackageBase: string | null = null;

        // The last URL known for each frame, because the location of a cross-origin document can't be read.
        private static readonly lastKnownUrls = new WeakMap<HTMLIFrameElement, string>();

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
            // A cross-origin document has no contentDocument.
            return (<HTMLIFrameElement>document.getElementById(htmlId)).contentDocument?.title ?? "";
        }

        static setAttribute(htmlId: string, name: string, value: string) {
            (<HTMLIFrameElement>document.getElementById(htmlId)).setAttribute(name, value);
        }

        static getAttribute(htmlId: string, name: string): string {
            return (<HTMLIFrameElement>document.getElementById(htmlId)).getAttribute(name);
        }

        static navigate(htmlId: string, url: string) {
            const iframe = document.getElementById(htmlId) as HTMLIFrameElement;
            if (iframe) {
                WebView.lastKnownUrls.set(iframe, WebView.toAbsoluteUrl(url));
                try {
                    if (iframe.contentWindow) {
                        iframe.contentWindow.location.href = url;
                    }
                } catch {
                    // Fall back to setAttribute if contentWindow access fails (cross-origin)
                    iframe.setAttribute("src", url);
                }
            }
        }

        static navigateToString(htmlId: string, html: string) {
            const iframe = document.getElementById(htmlId) as HTMLIFrameElement;
            if (iframe) {
                // The URL of a srcdoc document, which the browser hides in a sandboxed frame. It's recorded before srcdoc is set,
                // so a load that follows right away finds it.
                WebView.lastKnownUrls.set(iframe, "about:srcdoc");
                iframe.setAttribute("srcdoc", html);
            }
        }

        private static readUrl(iframe: HTMLIFrameElement): { url: string | null, isCrossOrigin: boolean } {
            try {
                return { url: iframe.contentWindow.location.href, isCrossOrigin: false };
            } catch (e) {
                if ((e as DOMException)?.name !== "SecurityError") {
                    throw e;
                }

                // The browser hides the location of a cross-origin document (another site, a data: URL, a sandboxed page).
                return { url: WebView.lastKnownUrls.get(iframe) ?? null, isCrossOrigin: true };
            }
        }

        // Resolves a URL as navigating the frame does, since a virtual host mapping navigates to a path relative to the app.
        private static toAbsoluteUrl(url: string): string {
            try {
                return new URL(url, document.baseURI).href;
            } catch {
                // The browser can't navigate to it either.
                return url;
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
            const { url, isCrossOrigin } = WebView.readUrl(iframe);
            if (!isCrossOrigin && url !== null) {
                WebView.lastKnownUrls.set(iframe, url);
            }

            WebView.unoExports.DispatchLoadEvent(iframe.id, url);

            if (isCrossOrigin) {
                // Such a document can't be hooked for NewWindowRequested either.
                return;
            }

            try {
                if (iframe.contentWindow && WebView.unoExports.DispatchNewWindowRequested) {
                    
                    const unoExports = WebView.unoExports;
                    
                    if (!(iframe.contentWindow as any).__unoOpenOverridden) {
                        if (!(iframe.contentWindow as any).__unoOriginalOpen) {
                            (iframe.contentWindow as any).__unoOriginalOpen = iframe.contentWindow.open;
                        }
                        iframe.contentWindow.open = function(url?: string, target?: string, features?: string) {
                            const referer = iframe.contentWindow.location.href;

                            const handled = unoExports.DispatchNewWindowRequested(
                                iframe.id,
                                url || '',
                                referer
                            );

                            if (!handled) {
                                return (iframe.contentWindow as any).__unoOriginalOpen.call(this, url, target, features);
                            }
                            
                            return null;
                        };
                        (iframe.contentWindow as any).__unoOpenOverridden = true;
                    }

                    iframe.contentDocument.addEventListener('click', (e: MouseEvent) => {
                        const target = e.target as HTMLElement;
                        const link = target.closest('a[target="_blank"]') as HTMLAnchorElement;
                        if (link) {
                            const targetUrl = link.href;
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
                        }
                    });
                }
            } catch (e) {
                // Cross-origin documents were skipped above, so a failure here is unexpected.
                console.warn("Uno.WebView: Could not attach NewWindowRequested handlers.", e);
            }
        }
    }
}
