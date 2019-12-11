# Sample NURAPI applications for C#

## NurAPI framework

The framework provides an interface that works as a bridge between the NurAPI and a transport that is used for communication with the reader device. ''DeviceDiscoverer'' is used for enumerating the devices that can be used with the ''NurAPI''.

## Supported platforms and transports
**NordicID.NurApi.Net** Common .NET Standard 1.2 API library
**NordicID.NurApi.Android** Xamarin Android (Bluetooth LE)  
**NordicID.NurApi.iOS** Xamarin iOS (Bluetooth LE)  
**NordicID.NurApi.DotnetFramework** .NET Framework 4.6.1 (Serial)  
**NordicID.NurApi.DotnetCore** .NET Core 1.1 (Serial)

## Using the framework

Include the ``NurAPi.Net`` and the platform specific transport dll or nuget to your project as a reference.  
Add the using statements ``using NordicID.NurApi.Net`` for NurAPI and e.g. ``using NordicID.NurApi.Android`` for platform specific transport

```csharp
using NordicID.NurApi.Net;
using NordicID.NurApi.Android;
```

Android requires location and bluetooth permission on the AndroidManifest.xml file.
```xml
	<uses-permission android:name="android.permission.BLUETOOTH" />
	<uses-permission android:name="android.permission.BLUETOOTH_ADMIN" />
	<uses-permission android:name="android.permission.BLUETOOTH_PRIVILEGED" />
	<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
	<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
```
Android also requires querying permission for the location from the user. This depends on what type the application is so it must be implemented into the app. For example for Xamarin Forms.
```csharp
using Android.Support.V4.Content;
using Android.Support.V4.App;

public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
.
.
.

List<string> _permission = new List<string>();

        private void RequestPermissionsManually()
        {
            try
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
                    _permission.Add(Manifest.Permission.AccessFineLocation);


                if (_permission.Count > 0)
                {
                    string[] array = _permission.ToArray();
                    ActivityCompat.RequestPermissions(this, array, array.Length);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        override public void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            //location || storage
            if (requestCode == 2 || requestCode == 5)
            {
                if (grantResults.Length == _permission.Count)
                {
                    for (int i = 0; i < requestCode; i++)
                    {
                        if (grantResults[i] == Permission.Granted)
                        {
                            //do nothing, we already have permissions granted
                        }
                        else
                        {
                            _permission = new List<string>();
                            RequestPermissionsManually();
                            break;
                        }
                    }
                }

            }
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
```

iOS requires Bluetooth usage descriptions to be added to Info.plist
```xml
	<key>NSBluetoothAlwaysUsageDescription</key>
	<string>Required for reader connection</string>
	<key>NSBluetoothPeripheralUsageDescription</key>
	<string>This app needs to access an RFID reader.</string>
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
public class MainActivity : Activity, NurApiListener
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


