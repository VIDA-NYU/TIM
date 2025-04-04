Steps:
- (Optional) Build the `../HLStreamerUnityPlugin` using `Release` and `ARM64`. Then copy the .dll and .winmd files from `../HLStreamerUnityPlugin/ARM64/Release/HLStreamerUnityPlugin/` into `Assets/Plugins/` folder of this Unity project. Note: In case the server or token addresses change, this step must be done.
- In the `Build Settings` of this Unity project, change `Platform` to `UWP`, then change `Target Device` to `HoloLens` and `Architecture` to `ARM64`.
- After building the Visual Studio solution from Unity, go to `[Build folder]/HLStreamerExample/Package.appxmanifest`and replace the `Package` part with follows, and replace the `Capabilities` part with follows.
```xml
<Package 
  xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest" 
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" 
  xmlns:uap2="http://schemas.microsoft.com/appx/manifest/uap/windows10/2" 
  xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3" 
  xmlns:uap4="http://schemas.microsoft.com/appx/manifest/uap/windows10/4" 
  xmlns:iot="http://schemas.microsoft.com/appx/manifest/iot/windows10" 
  xmlns:mobile="http://schemas.microsoft.com/appx/manifest/mobile/windows10" 
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities" 
  IgnorableNamespaces="uap uap2 uap3 uap4 mp mobile iot rescap" 
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"> 
```
```xml
<Capabilities>
    <rescap:Capability Name="perceptionSensorsExperimental"/>
    <uap2:Capability Name="spatialPerception"/>
	<Capability Name="internetClient" />
    <Capability Name="internetClientServer"/>
    <DeviceCapability Name="webcam"/>
	<DeviceCapability Name="backgroundSpatialPerception" />
    <DeviceCapability Name="microphone" />
    <DeviceCapability Name="gazeinput" />
</Capabilities>
```
- Build the app using `ARM64` in Visual Studio and deploy the app to HoloLens 2