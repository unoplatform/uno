---
uid: Uno.Development.SignalR
---

# How to integrate SignalR with an Uno Platform app

[ASP.NET Core SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction) is an open-source library that simplifies adding real-time web functionality to apps. It enables server-side code to push content to connected clients instantly using WebSockets, Server-Sent Events, or Long Polling (chosen automatically based on capabilities).

SignalR is a good fit for Uno Platform apps that need:

- Real-time messaging or chat
- Live dashboards and monitoring
- Collaborative editing
- Push notifications from a server

This guide walks you through creating an ASP.NET Core SignalR server and connecting to it from an Uno Platform client app. The SignalR .NET client (`Microsoft.AspNetCore.SignalR.Client`) works on all Uno target platforms — WebAssembly, Desktop (Windows, macOS, Linux via Skia), iOS, and Android.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) or later
- An Uno Platform app (created via `dotnet new unoapp` or the Uno Platform Visual Studio extension)
- Visual Studio 2022 17.8+, VS Code, or JetBrains Rider

> [!NOTE]
> An Azure account is **not** required. You can run the SignalR server locally during development. For production hosting options, see [Publish an ASP.NET Core SignalR app to Azure](https://learn.microsoft.com/aspnet/core/signalr/publish-to-azure-web-app) or [Azure SignalR Service](https://learn.microsoft.com/azure/azure-signalr/signalr-overview).

## Step 1 — Create the SignalR server

Create a new ASP.NET Core Web API project that will host the SignalR hub:

```dotnetcli
dotnet new web -n ChatServer
cd ChatServer
```

### Define a SignalR Hub

Create a `Hubs` folder and add a `ChatHub.cs` file:

```csharp
// Hubs/ChatHub.cs
using Microsoft.AspNetCore.SignalR;

namespace ChatServer.Hubs;

public class ChatHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
```

A [Hub](https://learn.microsoft.com/aspnet/core/signalr/hubs) is a high-level pipeline that allows clients and the server to call methods on each other. The `SendMessage` method above broadcasts a message to every connected client.

### Configure the server

Replace the contents of `Program.cs` with:

```csharp
// Program.cs
using ChatServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

// CORS is required when the Uno client runs on a different origin
// (e.g. Uno WASM served from a different port).
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            // In production, replace with your app's specific origin(s).
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("CorsPolicy");

app.MapHub<ChatHub>("/chatHub");

app.Run();
```

> [!IMPORTANT]
> When your Uno WASM app is served from a different origin than the SignalR server, CORS must be configured on the server. The example above uses `SetIsOriginAllowed(_ => true)` for development convenience. In production, restrict this to your app's actual origin(s) and use `AllowCredentials()` since SignalR requires it for certain transports.

Start the server:

```dotnetcli
dotnet run --project ChatServer
```

Note the URL displayed (for example, `https://localhost:5001`). You will use this in the client.

## Step 2 — Add SignalR to the Uno client

### Install the NuGet package

In your Uno Platform project, install the SignalR .NET client package:

```dotnetcli
cd YourUnoApp
dotnet add package Microsoft.AspNetCore.SignalR.Client
```

### Connect to the hub

The following example shows a minimal code-behind approach. You can adapt this to MVVM, Uno.Extensions, or any pattern your app uses.

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace YourUnoApp;

public sealed partial class ChatPage : Page
{
    private HubConnection? _connection;

    public ChatPage()
    {
        this.InitializeComponent();
        this.Loaded += ChatPage_Loaded;
        this.Unloaded += ChatPage_Unloaded;
    }

    private async void ChatPage_Loaded(object sender, RoutedEventArgs e)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:5001/chatHub")
            .WithAutomaticReconnect()
            .Build();

        // Listen for messages from the server
        _connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            // Marshal back to the UI thread
            DispatcherQueue.TryEnqueue(() =>
            {
                MessagesListView.Items.Add($"{user}: {message}");
            });
        });

        try
        {
            await _connection.StartAsync();
            StatusText.Text = "Connected";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Connection failed: {ex.Message}";
        }
    }

    private async void ChatPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        if (_connection is not null &&
            !string.IsNullOrWhiteSpace(UserTextBox.Text) &&
            !string.IsNullOrWhiteSpace(MessageTextBox.Text))
        {
            await _connection.InvokeAsync(
                "SendMessage",
                UserTextBox.Text,
                MessageTextBox.Text);

            MessageTextBox.Text = string.Empty;
        }
    }
}
```

### Add the UI (XAML)

```xml
<Page x:Class="YourUnoApp.ChatPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <StackPanel Padding="20" Spacing="8">
        <TextBlock x:Name="StatusText" Text="Connecting..." />

        <TextBox x:Name="UserTextBox" PlaceholderText="Your name" />
        <TextBox x:Name="MessageTextBox" PlaceholderText="Message" />
        <Button Content="Send" Click="SendButton_Click" />

        <ListView x:Name="MessagesListView" Height="300" />
    </StackPanel>
</Page>
```

## Step 3 — Run the app

1. Start the **ChatServer** project.
2. Run your **Uno app** on any target platform.
3. Enter a name and message, then click **Send**. The message is broadcast to all connected clients in real time.

## Handling reconnection

The `WithAutomaticReconnect()` call in the example above configures the client to retry after 0, 2, 10, and 30 seconds when the connection is lost. You can customize retry delays:

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:5001/chatHub")
    .WithAutomaticReconnect(new[]
    {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60)
    })
    .Build();
```

You can subscribe to connection state events:

```csharp
connection.Reconnecting += error =>
{
    DispatcherQueue.TryEnqueue(() => StatusText.Text = "Reconnecting...");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    DispatcherQueue.TryEnqueue(() => StatusText.Text = "Reconnected");
    return Task.CompletedTask;
};

connection.Closed += error =>
{
    DispatcherQueue.TryEnqueue(() => StatusText.Text = "Disconnected");
    return Task.CompletedTask;
};
```

For more details, see [Handle lost connection](https://learn.microsoft.com/aspnet/core/signalr/dotnet-client#handle-lost-connection).

## Platform-specific considerations

| Platform | Transport | Notes |
|----------|-----------|-------|
| WebAssembly | WebSockets | Requires CORS on the server. The `Microsoft.AspNetCore.SignalR.Client` NuGet package works in Uno WASM without additional configuration. |
| Desktop (Skia) | WebSockets | Works out of the box on Windows, macOS, and Linux. |
| iOS / Android | WebSockets | Works out of the box. Ensure the server URL is reachable from the device or emulator (avoid `localhost`; use the machine's IP or a tunnel such as [dev tunnels](https://learn.microsoft.com/azure/developer/dev-tunnels/overview)). |
| Windows (WinUI) | WebSockets | Works out of the box. |

## Sample app

The [ChatSignalR sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/ChatSignalR) in the Uno.Samples repository demonstrates a full chat application using SignalR with an Uno Platform client and an ASP.NET Core server.

## Further reading

- [Overview of ASP.NET Core SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction)
- [ASP.NET Core SignalR .NET Client](https://learn.microsoft.com/aspnet/core/signalr/dotnet-client)
- [Tutorial: Get started with ASP.NET Core SignalR](https://learn.microsoft.com/aspnet/core/tutorials/signalr)
- [SignalR Hubs](https://learn.microsoft.com/aspnet/core/signalr/hubs)
- [Azure SignalR Service](https://learn.microsoft.com/azure/azure-signalr/signalr-overview)
