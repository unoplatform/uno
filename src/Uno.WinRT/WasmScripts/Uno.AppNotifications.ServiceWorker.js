(function (scope) {
	const isServiceWorker = typeof ServiceWorkerGlobalScope !== "undefined" && scope instanceof ServiceWorkerGlobalScope;
	if (!isServiceWorker) {
		globalThis.UnoAppNotificationsServiceWorkerUrl = document.currentScript?.src ?? "";
		return;
	}
	const appId = new URL(scope.location.href).searchParams.get("uno-app-id");

	const messageClients = async message => {
		const windows = await scope.clients.matchAll({ type: "window", includeUncontrolled: true });
		for (const client of windows) {
			client.postMessage(message);
		}
		return windows;
	};

	scope.addEventListener("notificationclick", event => {
		const notification = event.notification;
		const data = notification.data?.unoAppNotification;
		if (!data || typeof appId !== "string" || data.appId !== appId) {
			return;
		}

		event.waitUntil((async () => {
			const action = Array.isArray(data.actions)
				? data.actions.find(candidate => candidate.id === event.action)
				: null;
			const token = event.action ? action?.activationToken : data.activationToken;
			if (typeof token !== "string") {
				return;
			}
			const activation = {
				type: "uno-app-notification-activated",
				id: data.id,
				appId,
				token,
			};
			try {
				const clientUrl = new URL(data.clientUrl || data.appBaseUrl);
				if (!clientUrl.href.startsWith(appId)) {
					return;
				}
				const windows = await scope.clients.matchAll({ type: "window", includeUncontrolled: true });
				for (const appWindow of windows.filter(client => client.url.startsWith(appId))) {
					if (await postActivation(appWindow, activation)) {
						await appWindow.focus();
						notification.close();
						return;
					}
				}

				const launchUrl = clientUrl;
				launchUrl.searchParams.set("uno-app-notification", token);
				const exactWindow = windows.find(client => client.url === data.clientUrl);
				if (exactWindow) {
					const navigated = await exactWindow.navigate(launchUrl.href);
					if (navigated) {
						await navigated.focus();
						notification.close();
						return;
					}
				}
				const opened = await scope.clients.openWindow(launchUrl.href);
				if (opened) {
					notification.close();
				}
			}
			catch (error) {
				console.warn("Unable to activate an app notification.", error);
			}
		})());
	});

	const postActivation = (client, activation) => new Promise(resolve => {
		const channel = new MessageChannel();
		let completed = false;
		const finish = accepted => {
			if (!completed) {
				completed = true;
				resolve(accepted);
			}
		};
		const timeout = scope.setTimeout(() => finish(false), 750);
		channel.port1.onmessage = message => {
			scope.clearTimeout(timeout);
			finish(message.data?.accepted === true);
		};
		client.postMessage(activation, [channel.port2]);
	});

	scope.addEventListener("notificationclose", event => {
		const data = event.notification.data?.unoAppNotification;
		if (data?.appId === appId) {
			event.waitUntil(messageClients({ type: "uno-app-notification-closed", id: data.id, appId }));
		}
	});
})(self);