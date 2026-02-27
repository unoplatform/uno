namespace Windows.Security.Authentication.Web {
	export class WebAuthenticationBroker {

		public static getReturnUrl() {
			return window.location.origin;
		}

		public static authenticateUsingIframe(
			iframeId: string,
			urlNavigate: string,
			urlRedirect: string,
			timeout: number
		) {
			return new Promise<string>((ok, err) => {
				let iframe: HTMLIFrameElement;

				if (iframeId) {
					iframe = document.getElementById(iframeId) as HTMLIFrameElement;
				}

				if (!iframe) {
					iframe = document.createElement("iframe");
					iframe.style.opacity = "0";
					iframe.style.pointerEvents = "none";
					document.body.append(iframe);
				}

				const terminate = () => {
					iframe.removeEventListener("load", onload);
					iframe.src = "about:blank";
					if (!iframeId) {
						iframe.remove();
					}
				}

				const onload = () => {
					if (!iframe.contentDocument) {
						return; // can't access right now
					}

					const currentUrl = iframe.contentDocument.URL;
					console.log("iframe src=" + currentUrl);

					if (currentUrl.indexOf(urlRedirect) === 0) {
						terminate();
						ok(`success|${currentUrl}`);
					}
				};

				iframe.addEventListener("load", onload);

				iframe.src = urlNavigate;
			})
				;
		}

		public static authenticateUsingWindow(
			urlNavigate: string,
			urlRedirect: string,
			title: string,
			popUpWidth: number,
			popUpHeight: number,
			timeout: number): Promise<string> {

			let win: Window = null;
			let timerSubscription: number = null;

			return new Promise<string>((ok, err) => {

				let finished = false;
				let closing = false;
				const close = () => {
					if (closing) {
						return; // Prevent re-entry when win.close() triggers beforeunload
					}
					closing = true;

					if (timerSubscription) {
						window.clearInterval(timerSubscription);
						timerSubscription = null;
					}

					if (win) {
						const w = win;
						win = null;
						w.close();
					}

					if (!finished) {
						err("Incomplete");
					}
				};

				const completeSuccessfully = (url: string) => {
					if (!finished) {
						ok(`success|${url}`);
						finished = true;
						close();
					}
				};

				const completeUserCancel = () => {
					if (!finished) {
						ok(`cancel`);
						finished = true;
						close();
					}
				}

				const completeTimeout = () => {
					if (!finished) {
						ok(`timeout`);
						finished = true;
						close();
					}
				}

				const completeWithError = (error: string) => {
					if (!finished) {
						err(error);
						finished = true;
						close();
					}
				};

				try {
					/**
					 * adding winLeft and winTop to account for dual monitor
					 * using screenLeft and screenTop for IE8 and earlier
					 */
					const winLeft = window.screenLeft ? window.screenLeft : window.screenX;
					const winTop = window.screenTop ? window.screenTop : window.screenY;
					/**
					 * window.innerWidth displays browser window"s height and width excluding toolbars
					 * using document.documentElement.clientWidth for IE8 and earlier
					 */
					const width = window.innerWidth ||
						document.documentElement.clientWidth ||
						document.body.clientWidth;
					const height = window.innerHeight ||
						document.documentElement.clientHeight ||
						document.body.clientHeight;
					const left = ((width / 2) - (popUpWidth / 2)) + winLeft;
					const top = ((height / 2) - (popUpHeight / 2)) + winTop;

					// Listen for postMessage from the popup (more reliable in Safari)
					const messageHandler = (event: MessageEvent) => {
						// Verify the message is from our expected redirect origin
						if (event.origin === window.location.origin ||
							urlRedirect.startsWith(event.origin)) {
							const data = event.data;
							if (typeof data === 'string' && data.indexOf(urlRedirect) === 0) {
								window.removeEventListener('message', messageHandler);
								completeSuccessfully(data);
							} else if (data && typeof data === 'object' && data.type === 'oauth-callback') {
								window.removeEventListener('message', messageHandler);
								if (data.url && data.url.indexOf(urlRedirect) === 0) {
									completeSuccessfully(data.url);
								}
							}
						}
					};
					window.addEventListener('message', messageHandler);

					// open the window
					win = window.open(urlNavigate,
						title,
						"width=" + popUpWidth + ", height=" + popUpHeight + ", top=" + top + ", left=" + left);
					if (!win) {
						window.removeEventListener('message', messageHandler);
						completeWithError("Can't open window");
						return;
					}
					if (win.focus) {
						win.focus();
					}

					const onFinalUrlReached = (success: boolean, timedout: boolean, finalUrlOrMessage: string) => {
						window.removeEventListener('message', messageHandler);
						if (success) {
							completeSuccessfully(finalUrlOrMessage);
						} else {
							if (timedout) {
								completeTimeout();
							} else {
								completeUserCancel();
							}
						}
					}
					timerSubscription = this.startMonitoringRedirect(win, urlRedirect, timeout, onFinalUrlReached);
				} catch (e) {
					completeWithError(`${e}`);
				}
			});
		}

		private static startMonitoringRedirect(
			win: Window,
			urlRedirect: string,
			timeout: number,
			callback: (success: boolean, timedout: boolean, finalUrlOrMessage: string) => void): number {

			const currentTime = (new Date()).getTime();
			const maxTime = currentTime + timeout;
			let callbackInvoked = false;

			const invokeCallback = (success: boolean, timedout: boolean, finalUrlOrMessage: string) => {
				if (!callbackInvoked) {
					callbackInvoked = true;
					callback(success, timedout, finalUrlOrMessage);
				}
			};

			const subscription = window.setInterval(() => {
					try {
						if ((new Date()).getTime() > maxTime) {
							invokeCallback(false, true, null);
							return;
						}

						// Check if window is closed - this should be accessible even cross-origin
						let isClosed = false;
						try {
							isClosed = win.closed;
						} catch (e) {
							// Some browsers restrict even this for cross-origin, continue polling
							return;
						}

						if (isClosed) {
							invokeCallback(false, false, "Popup closed");
							return;
						}

						// Try multiple methods to get the URL - Safari is very strict about cross-origin access
						// Method 1: Try to check if we're same-origin by comparing origin
						let isSameOrigin = false;
						try {
							// This will throw if cross-origin
							isSameOrigin = win.location.origin === window.location.origin;
						} catch (e) {
							// Cross-origin, continue polling
							return;
						}

						if (!isSameOrigin) {
							return;
						}

						// Method 2: Now that we know we're same-origin, get the full URL
						let url: string = null;
						try {
							url = win.location.href;
						} catch (e) {
							// Safari ITP may still block even same-origin in some cases
							// Try alternative method: string coercion
							try {
								url = '' + win.location;
							} catch (e2) {
								return;
							}
						}

						if (url && url.indexOf(urlRedirect) === 0) {
							invokeCallback(true, false, url);
						}
					} catch (e) {
						// Catch-all for any unexpected errors - continue polling
					}
				},
				100);

			return subscription;
		}
	}
}
