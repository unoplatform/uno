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
            if (iframe) {
                try {
                    if (iframe.contentWindow) {
                        iframe.contentWindow.location.href = url;
                    }
                } catch (e) {
                    // Fall back to setAttribute if contentWindow access fails (cross-origin)
                    iframe.setAttribute("src", url);
                }
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

			const isMultithreaded = (<any>globalThis).Uno.UI.Runtime.Skia.WebAssemblyThreading.isThreadingEnabled();

			if (isMultithreaded) {
				WebView.unoExports.DispatchLoadEventAsync(iframe.id, absoluteUrl);
			} else {
				WebView.unoExports.DispatchLoadEvent(iframe.id, absoluteUrl);
			}
			
            try {
                if (iframe.contentWindow) {
                    
                    const unoExports = WebView.unoExports;
                    
                    if (!(iframe.contentWindow as any).__unoOpenOverridden) {
                        if (!(iframe.contentWindow as any).__unoOriginalOpen) {
                            (iframe.contentWindow as any).__unoOriginalOpen = iframe.contentWindow.open;
                        }
                        iframe.contentWindow.open = function(url?: string, target?: string, features?: string) {
                            const referer = iframe.contentWindow.location.href;

							if (isMultithreaded) {
								unoExports.DispatchNewWindowRequestedAsync(iframe.id, url || '', referer)
									.then((handled: boolean) => {
										if (!handled) {
											(iframe.contentWindow as any).__unoOriginalOpen.call(this, url, target, features);
										}
									});
							} else {
								const handled = unoExports.DispatchNewWindowRequested(
									iframe.id,
									url || '',
									referer
								);

								if (!handled) {
									return (iframe.contentWindow as any).__unoOriginalOpen.call(this, url, target, features);
								}
							}
                            
							// In MT, we always return null since we cannot know whether DispatchNewWindowRequested
							// was handled or not before this call returns.
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

							if (isMultithreaded) {
								// In MT, we always preventDefault()/stopPropagation(), if the click wasn't handled we play it back.
								e.preventDefault();
								e.stopPropagation();

								unoExports.DispatchNewWindowRequestedAsync(iframe.id, targetUrl, referer)
									.then((handled: boolean) => {
										if (!handled) {
											(iframe.contentWindow as any).__unoOriginalOpen.call(iframe.contentWindow, targetUrl, '_blank');
										}
									});

								return;
							}

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
                // This can fail if the iframe content is cross-origin.
                // We log this as a warning, as it's a known browser security feature.
                // https://developer.mozilla.org/en-US/docs/Web/Security/Same-origin_policy
                console.warn("Uno.WebView: Could not attach NewWindowRequested handlers. This is expected if the iframe content is cross-origin.", e);
            }
        }
    }
}
