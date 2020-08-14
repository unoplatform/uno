# Uno Chat

Uno Chat++ is a real-time chat service using SignalR that can be used on UWP, WebAssembly, iOS, Android, MacOS, and as a console app. 

This tutorial will walk through creating this app on Visual Studio, originally created by [Ian Bebbington](https://ian.bebbs.co.uk/).

## Prerequisites 

* Visual Studio 2019
* Azure account 

## SignalR

[Create the SingalR service](/articles/signalr.md)

## Console App 

1. Create the `UnoChat.Clinet.Console` project t to the same solution as your SignalR service. 

2. Install the `Microsoft.AspNetCore.SignalR.Client` Nuget package to the `UnoChat.Client.Console` project only.

3. In your `Main.cs` file, replace the default code with the following: 

``` csharp
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace UnoChat.Client.Console
{
    using Console = System.Console;

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hi! Err... who are you?");

            var name = Console.ReadLine();

            Console.WriteLine($"Ok {name} one second, we're going to connect to the SignalR server...");

            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:61877/ChatHub")
                .WithAutomaticReconnect()
                .Build();

            connection.On<string, string>("ReceiveMessage", (user, message) => Console.WriteLine($"{user}: {message}"));

            await connection.StartAsync();

            Console.WriteLine($"Aaaaaand we're connected. Enter a message and hit return to send it to other connected clients...");

            while (true)
            {
                var message = Console.ReadLine();

                await connection.InvokeAsync("SendMessage", name, message);
            }
        }
    }
}
```

4. In your `UnoChat.Service` project, navigate to `Properties > Debug > Web Server Settings` and set the `App URL` to `.WithUrl("http://localhost:61877/ChatHub")`

#### Test Locally

1. Right click on the `UnoChat.Service` project and select `Debug > Start New Instance`

2. While the service is running, right click on the `UnoChat.Client.Console` project and select `Debug > Start New Instance`

#### Deploy 

1. Right click on the `UnoChat.Service` project and select `Publish`

2. Click through prompts to publish to Azure

3. Test the deployment by exchanging our `App URL` with `URL` provided to you in Azure. It should look something like this: `.WithUrl("https://unochatservice20200716114254.azurewebsites.net/ChatHub")`

## Adding Uno

1. Right click on the `UnoChat` solution and select `Add > New Project` 

2. Select `Cross-Platform App (Uno Platform)` and name the project `UnoChat.Client`

3. Install the following Nuget packages to all the Uno project heads:

    * `Uno.UI v3.0.5`
    * `Uno.UI.RemoteControl v3.0.5`
    * `Uno.Wasm.Bootstrap v1.3.4`
    * `Uno.Wasm.Bootstrap.DevServer v1.3.4`
    * `Microsoft.AspNetCore.SignalR.Client v3.1.7`
    * `MVx.Obserable v2.0.0` 

4. Navigate to `Properties > Root namespace` and update the value from `UnoChat.Client.Shared` to `UnoChat.Client`

## Adding `MVx.Observable`

1. Add a `Schedule.cs` file in the `UnoChat.Client.Shared` project

``` csharp
using System;
using System.Reactive.Concurrency;
using System.Threading;

namespace UnoChat.Client
{
    public static partial class Schedulers
    {
        static partial void OverrideDispatchScheduler(ref IScheduler scheduler);

        private static readonly Lazy<IScheduler> DispatcherScheduler = new Lazy<IScheduler>(
            () =>
            {
                IScheduler scheduler = null;

                OverrideDispatchScheduler(ref scheduler);

                return scheduler == null
                    ? new SynchronizationContextScheduler(SynchronizationContext.Current)
                    : scheduler;
            }
        );

        public static IScheduler Dispatcher => DispatcherScheduler.Value;

        public static IScheduler Default => Scheduler.Default;
    }
}
```

2. Add a partial class in the `UnoChat.Client.UWP` project by adding a `Schedulers.cs` file and adding the following ovverride:

``` csharp
using System.Reactive.Concurrency;
using Windows.UI.Xaml;

namespace UnoChat.Client
{
    public static partial class Schedulers
    {
        static partial void OverrideDispatchScheduler(ref IScheduler scheduler)
        {
            scheduler = new CoreDispatcherScheduler(Window.Current.Dispatcher);
        }
    }
}
```

## Adding a ViewModel

1. Create a `ViewModel` class in the `UnoChat.Client.Shared` project

``` csharp
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Uno.Extensions;

namespace UnoChat.Client
{
    public class ViewModel : INotifyPropertyChanged
    {
        private readonly MVx.Observable.Property<string> _name;
        private readonly MVx.Observable.Property<HubConnectionState> _state;
        private readonly MVx.Observable.Command _connect;

        private readonly MVx.Observable.Property<string> _lastMessageReceived;
        private readonly MVx.Observable.Property<IEnumerable<string>> _allMessages;
        private readonly MVx.Observable.Property<string> _messageToSend;
        private readonly MVx.Observable.Property<bool> _messageToSendIsEnabled;
        private readonly MVx.Observable.Command _sendMessage;

        private readonly HubConnection _connection;

        public event PropertyChangedEventHandler PropertyChanged;

        private static string DefaultName => typeof(ViewModel)
            .Assembly
            .GetName()
            .Name
            .Split('.')
            .Last();

        public ViewModel()
        {
            _name = new MVx.Observable.Property<string>(DefaultName, nameof(Name), args => PropertyChanged?.Invoke(this, args));
            _state = new MVx.Observable.Property<HubConnectionState>(HubConnectionState.Disconnected, nameof(State), args => PropertyChanged?.Invoke(this, args));
            _connect = new MVx.Observable.Command();
            _lastMessageReceived = new MVx.Observable.Property<string>(nameof(LastMessageReceived), args => PropertyChanged?.Invoke(this, args));
            _allMessages = new MVx.Observable.Property<IEnumerable<string>>(Enumerable.Empty<string>(), nameof(AllMessages), args => PropertyChanged?.Invoke(this, args));
            _messageToSend = new MVx.Observable.Property<string>(nameof(MessageToSend), args => PropertyChanged?.Invoke(this, args));
            _messageToSendIsEnabled = new MVx.Observable.Property<bool>(false, nameof(MessageToSendIsEnabled), args => PropertyChanged?.Invoke(this, args));
            _sendMessage = new MVx.Observable.Command();

            _connection = new HubConnectionBuilder()
                .WithUrl("https://unochatservice20200716114254.azurewebsites.net/ChatHub")
                .WithAutomaticReconnect()
                .Build();
        }

        private IDisposable ShouldEnableConnectWhenNotConnected()
        {
            return _state
                .Select(state => state == HubConnectionState.Disconnected)
                .ObserveOn(Schedulers.Dispatcher)
                .Subscribe(_connect);
        }

        private IDisposable ShouldEnableMessageToSendWhenConnected()
        {
            return _state
                .Select(state => state == HubConnectionState.Connected)
                .Subscribe(_messageToSendIsEnabled);
        }

        private IDisposable ShouldConnectToServiceWhenConnectInvoked()
        {
            return _connect
                .SelectMany(_ => Observable
                    .StartAsync(async () =>
                    {
                        await _connection.StartAsync();
                        return _connection.State;
                    }))
                .ObserveOn(Schedulers.Dispatcher)
                .Subscribe(_state);
        }

        private IDisposable ShouldDisconnectFromServiceWhenDisposed()
        {
            return Disposable.Create(() => _ = _connection.StopAsync());
        }

        private IDisposable ShouldListenForNewMessagesFromTheService()
        {
            return Observable
                .Create<string>(
                    observer =>
                    {
                        Action<string, string> onReceiveMessage =
                            (user, message) => observer.OnNext($"{user}: {message}");

                        return _connection.On("ReceiveMessage", onReceiveMessage);
                    })
                .ObserveOn(Schedulers.Dispatcher)
                .Subscribe(_lastMessageReceived);
        }

        private IDisposable ShouldAddNewMessagesToAllMessages()
        {
            return _lastMessageReceived
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .WithLatestFrom(_allMessages, (message, messages) => messages.Concat(message).ToArray())
                .Subscribe(_allMessages);
        }

        private IDisposable ShouldEnableSendMessageWhenConnectedAndBothNameAndMessageToSendAreNotEmpty()
        {
            return Observable
                .CombineLatest(_state, _name, _messageToSend, (state, name, message) => state == HubConnectionState.Connected && !(string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(message)))
                .Subscribe(_sendMessage);
        }

        private IDisposable ShouldSendMessageToServiceThenClearSentMessage(IObservable<object> messageToSendBoxReturn)
        {
            var namedMessage = Observable
                .CombineLatest(_name, _messageToSend, (name, message) => (Name: name, Message: message));

            return Observable.Merge(_sendMessage, messageToSendBoxReturn)
                .WithLatestFrom(namedMessage, (_, tuple) => tuple)
                .Where(tuple => !string.IsNullOrEmpty(tuple.Message))
                .SelectMany(tuple => Observable
                    .StartAsync(() => _connection.InvokeAsync("SendMessage", tuple.Name, tuple.Message)))
                .Select(_ => string.Empty)
                .ObserveOn(Schedulers.Dispatcher)
                .Subscribe(_messageToSend);
        }

        public IDisposable Activate(IObservable<object> messageToSendBoxReturn)
        {
            return new CompositeDisposable(
                ShouldEnableConnectWhenNotConnected(),
                ShouldEnableMessageToSendWhenConnected(),
                ShouldConnectToServiceWhenConnectInvoked(),
                ShouldDisconnectFromServiceWhenDisposed(),
                ShouldListenForNewMessagesFromTheService(),
                ShouldAddNewMessagesToAllMessages(),
                ShouldEnableSendMessageWhenConnectedAndBothNameAndMessageToSendAreNotEmpty(),
                ShouldSendMessageToServiceThenClearSentMessage(messageToSendBoxReturn)
            );
        }

        public string Name
        {
            get => _name.Get();
            set => _name.Set(value);
        }

        public HubConnectionState State => _state.Get();

        public string LastMessageReceived => _lastMessageReceived.Get();

        public IEnumerable<string> AllMessages => _allMessages.Get();

        public string MessageToSend
        {
            get => _messageToSend.Get();
            set => _messageToSend.Set(value);
        }

        public bool MessageToSendIsEnabled => _messageToSendIsEnabled.Get();

        public ICommand Connect => _connect;

        public ICommand SendMessage => _sendMessage;
    }
}
```

## Adding a View

1. Add the following code to the `MainView.cs` code behind

```csharp
using System;
using System.Reactive.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UnoChat.Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly ViewModel _viewModel;
        private IDisposable _behaviours;

        public MainPage()
        {
            this.InitializeComponent();

            _viewModel = new ViewModel();
            DataContext = _viewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var messageToSendReturn = Observable
                .FromEvent<KeyEventHandler, KeyRoutedEventArgs>(
                    handler => (s, k) => handler(k),
                    handler => MessageToSendTextBox.KeyUp += handler,
                    handler => MessageToSendTextBox.KeyUp -= handler)
                .Where(k => k.Key == Windows.System.VirtualKey.Enter);

            _behaviours = _viewModel.Activate(messageToSendReturn);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            if (_behaviours != null)
            {
                _behaviours.Dispose();
                _behaviours = null;
            }
        }
    }
}
```

2. Add the following code to the `MainView.xaml` file

``` xaml
<Page
    x:Class="UnoChat.Client.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:UnoChat.Client"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>
        <TextBlock Text="Name:" Style="{StaticResource BaseTextBlockStyle}" Grid.Row="0" Grid.Column="0" Margin="4" VerticalAlignment="Center" />
        <TextBox Text="{Binding Path=Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Grid.Row="0" Grid.Column="1" Margin="4"/>
        <Button Command="{Binding Path=Connect}" Content="Connect" Grid.Row="0" Grid.Column="2" Margin="4" Padding="16,4" HorizontalAlignment="Stretch"/>
        <ItemsControl ItemsSource="{Binding Path=AllMessages}" Grid.Row="1" Grid.ColumnSpan="3" Margin="4" />
        <TextBlock Text="Message:" Style="{StaticResource BaseTextBlockStyle}" Grid.Row="2" Grid.Column="0" Margin="4" VerticalAlignment="Center"/>
        <TextBox x:Name="MessageToSendTextBox" Text="{Binding Path=MessageToSend, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" IsEnabled="{Binding Path=MessageToSendIsEnabled}" Grid.Row="2" Grid.Column="1" Margin="4"/>
        <Button Command="{Binding Path=SendMessage}" Content="Send" Grid.Row="2" Grid.Column="2" Margin="4" Padding="16,4" HorizontalAlignment="Stretch"/>
    </Grid>
</Page>
```

## Test

1. Select the `UnoChat.Client.UWP` head as the startup project and run

2. To run another project head at the same time, right click that project head and select `Debug > Start New Instance`. Repeat for all project heads

3. For the `UnoChat.Client.Wasm` project head, add the following code to the `LinkerConfig.xaml` file before running

``` xaml
<linker>
  <assembly fullname="UnoChat.Client.Wasm" />
  <assembly fullname="Uno.UI" />

  <assembly fullname="Microsoft.AspnetCore.Http.Connections.Client"/>
  <assembly fullname="Microsoft.Extensions.Options"/>
  <assembly fullname="Microsoft.AspNetCore.SignalR.Client"/>
  <assembly fullname="Microsoft.AspNetCore.SignalR.Client.Core"/>
  <assembly fullname="Microsoft.AspNetCore.SignalR.Protocols.Json"/>

  <assembly fullname="System.Core">
	<!-- This is required by JSon.NET and any expression.Compile caller -->
	  <type fullname="System.Linq.Expressions*" />
  </assembly>
</linker>
```
