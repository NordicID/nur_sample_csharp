# Sample applications for NordicID NurApi C# library

## NurApi framework

The NurApi library is API framework in between your application and the physical RFID reader hardware. Different transports can be used for communication with the reader device.
''DeviceDiscoverer'' is used for enumerating the devices that can be used with the ''NurApi'.

## Supported platforms and transports
- **NordicID.NurApi.Net**
	- Common multiplatform .NET Standard 2.0 library
	- Provides tcp/ip transport and mdns device discovery
- **NordicID.NurApi.Android** 
	- Provides Android device Bluetooth LE transport and discovery
	- Can be used with .NET MAUI, Blazor, xamarin frameworks
- **NordicID.NurApi.SerialTransport**
	- Multiplatform .NET Standard 2.0 library, uses System.IO.Ports assembly
	- Provides serial port transport and discovery
- **NordicID.NurApi.SerialTransport.UWP**
	- Library for windows UWP apps
	- Provides serial port transport and discovery

## Documentation
API documentation is available at [nordicid.github.io/nur_sample_csharp/](https://nordicid.github.io/nur_sample_csharp/)

## Samples
- Simple multiplatform console app
	- [NurApiDocSample](NurApiDocSample)
- Mobile multiplatform sample
	- https://github.com/NordicID/rfiddemo_xamarin
- AvalnoniaUI for FR22 (multiplatform) sample
	- https://github.com/NordicID/fr22_samples/tree/master/5.%20AvaloniaUISample
- BlazorServerSample for FR22 (multiplatform) sample
	- https://github.com/NordicID/fr22_samples/tree/master/6.%20BlazorServerAppSample
