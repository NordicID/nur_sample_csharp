# Sample NURAPI applications for C#

## NurAPI framework

The framework provides an interface that works as a bridge between the NurAPI and a transport that is used for communication with the reader device. ''DeviceDiscoverer'' is used for enumerating the devices that can be used with the ''NurAPI''.

## Supported platforms and transports
**NordicID.NurApi.Android** Xamarin Android (Bluetooth LE)  
**NordicID.NurApi.iOS** Xamarin iOS (Bluetooth LE)  
**NordicID.NurApi.DotnetFramework** .NET Framework 4.6.1 (USB)  
**NordicID.NurApi.DotnetCore** .NET Core 1.1 (USB)

## Using the framework

Include the ``NurAPi.Net`` and the platform specific transport dll or nuget to your project as a reference.  
Add the using statements ``using NordicID.NurApi.Net`` for NurAPI and e.g. ``using NordicID.NurApi.Android`` for platform specific transport

```csharp
using NordicID.NurApi.Net;
using NordicID.NurApi.Android;
```

## Discovering devices

Create the ``DeviceDiscoverer`` instance and initialize it with the app context

```csharp
NurDeviceDiscovery dd = new NurDeviceDiscovery();
dd.Init(Application.Context);
```

Add a listener for discovered devices

```csharp
dd.DeviceDiscoveredEvent += OnDeviceDiscoverEvent;

private void OnDeviceDiscoverEvent(IDeviceDiscover instance, Uri uri, bool visible)
{
  Console.WriteLine("device found " + uri);
}
```

## Connecting the API

Create the ``NurApi`` instance, initialize it, set a ``NurApiListener`` and pass the uri to the Connect method

```csharp
NurApi api = new NurApi();
api.Init(Application.Context);
api.SetListener(this);
api.Connect(uri);
```

```csharp
public class MainActivity : Activity, *NurApiListener*
    {
        
```

Connection status is passed using ``StatusChangedEvent``

```csharp
public void StatusChangedEvent(NurTransport.Status status)
{
  Console.WriteLine("connection status: " + status.ToString());
}
```

## Sending commands to API

Most of the commands have ``Execute`` and ``ExecuteAsync`` methods that can be used to execute commands.

```csharp
CancellationTokenSource ct = new CancellationTokenSource();
NurCmdPing pingCmd = new NurCmdPing();
var resp = await pingCmd.ExecuteAsync(api, ct);
Console.WriteLine("ping response: " + resp);
```

All commands can be executed with API ``ExecuteCommand`` or ``ExecuteCommandAsync`` methods

```csharp
CancellationTokenSource ct = new CancellationTokenSource();
NurCmdPing pingCmd = new NurCmdPing();
var respCmd = (NurCmdPing) await api.ExecuteCommandAsync(pingCmd, ct);
var resp = respCmd.GetResponse();
Console.WriteLine("ping response: " + resp);
```


