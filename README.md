👨🏻‍🍳 SousVide
===

*Bluetooth client library and protocol specification for Anova Precision Cooker sous vide.*

## Background

The original [Anova Precision Cooker 1.0](https://www.amazon.com/Anova-Culinary-Precision-Bluetooth-Included/dp/B00UKPBXM4/) is an 800 watt sous vide released in 2014. You can control it either with physical on-device inputs or with a [phone app](https://play.google.com/store/apps/details?id=com.anovaculinary.android) over Bluetooth LE. Unlike later models, it does not have a Wi-Fi tranceiver.

Unfortunately, [Anova chose to charge new users $2/month or $10/year (USD) after 2024-08-21 to control their sous vides from the phone app](https://www.theverge.com/2024/8/19/24223878/anova-sous-vide-kitchen-app-subscription). Also, [they chose to remove Bluetooth connectivity from their app on 2025-09-28](https://arstechnica.com/gadgets/2024/08/smart-sous-vide-cooker-to-start-charging-2-month-for-10-year-old-companion-app/), so the only way to use these devices will be with the physical controls.

To be clear, **the remote control options for these sous vides are useless**. There is no advantage to starting, monitoring, or stopping cooking with your phone:
- It's far clumsier and slower than the physical dial and capacitive button.
- You're going to be near the device when it starts anyway.
- The phone app does not cook better than manual inputs because the only relevant parameters are the target water temperature and immediately starting cooking.
- The timer is useless because the entire point of sous vide is that it can run forever, so if you want to know when two hours have elapsed, set a timer on your phone or oven.
- If you want to sous vide as quickly as possible, the only solution is to use a continuous thermometer like the [Thermoworks Dot](https://www.thermoworks.com/dot/).
- Being able to transfer online "recipes" is a waste of time because they are so simplistic that they just set a useless timer and a target water temperature that you could have easily set yourself, either by looking it up on your [handy temperature guide](https://www.thermoworks.com/content/pdf/chef_recommended_temps.pdf) or a quick web search.
- There is no reason to check the water temperature remotely, because it will always be at the target temperature thanks to the sous vide running.

If you *still* want to control your Anova Precision Cooker over Bluetooth after 2025-09-28, you can try the following techniques until one works.
1. Install an [older version of the app](https://apkpure.com/anova-culinary/com.anovaculinary.android/download/3.5.1) that still has the Bluetooth functionality
1. Use the sample app in this repository
1. Write your own .NET program using the [client library in this repository](https://www.nuget.org/packages/SousVide/)
1. Write your own program in a programming language of your choice by following the [protocol specification](https://github.com/Aldaviva/SousVide/wiki/Communication-Protocol) in this repository
1. [Inspect Bluetooth LE traffic](https://github.com/Aldaviva/SousVide/wiki/Bluetooth-Low-Energy-Interception) between your phone and sous vide to understand and implement the GATT communication protocol yourself (it's pretty simple, just RPC-style string writes and response callbacks).

## Prerequisites
- Anova sous vide
    - Tested with [Precision Cooker 1.0 with Bluetooth](https://www.amazon.com/Anova-Culinary-Precision-Bluetooth-Included/dp/B00UKPBXM4/) (2014, 800W)
- Bluetooth adapter
    - Tested with [Asus USB-BT400](https://www.asus.com/us/networking-iot-servers/adapters/all-series/usbbt400/) and [Intel AX211](https://ark.intel.com/content/www/us/en/ark/products/204837/intel-wi-fi-6e-ax211-gig.html)
- .NET Framework 4.6.2, 6.0, or later

## Usage
### Bluetooth Pairing
1. Plug in the sous vide
1. Press the Bluetooth capacitive button to enter pairing mode. It will turn blue and start blinking.
1. Find the device in the Bluetooth settings of your operating system
    - Windows 10:
        1. Go to Settings › Devices › Bluetooth & other devices > Add Bluetooth or other device › Bluetooth
        1. Select Anova
    - Windows 11:
        1. Go to Settings › Bluetooth & devices › Devices
        1. Set Device settings › Bluetooth devices discovery to Advanced
        1. Select Add device › Bluetooth
        1. Select Anova
1. Enter the PIN `0000`

### Command Line Interface
TODO

### .NET Library
#### Getting Started
1. Add a depenency on the NuGet package [`SousVide`](https://www.nuget.org/packages/SousVide/).
    ```sh
    dotnet add package SousVide
    ```
1. When targeting Windows, add a Windows 10 TFM to the `<TargetFrameworks>` in your `.csproj` project file, otherwise it will use the Linux implementation.
    ```xml
    <TargetFrameworks>net8.0-windows10.0.19041.0</TargetFrameworks>
    ```
1. Connect to the paired sous vide.
    ```cs
    await using ISousVide? sousVide = await AnovaPrecisionCooker.Create();
    ```
1. Call methods, and read or subscribe to [reactive properties](https://www.nuget.org/packages/KoKo), on the `ISousVide` instance.
    ```cs
    Console.WriteLine($"{(sousVide.IsRunning.Value ? "Running" : "Stopped")}, desired temperature: {sousVide.DesiredTemperature.Value:N1}");
    ```

#### Application Programming Interface
`ISousVide` is the public interface of this library. Construct instances by calling `AnovaPrecisionCooker.Create(string?)`.

##### `DeviceId`
The unique ID of this Bluetooth device. Can be persisted and passed to `AnovaPrecisionCooker.Create(string?)` later to reconnect to the exact same device, in case there are multiple paired Precision Cookers.

##### `IsRunning`
`true` if the sous vide is cooking, or `false` otherwise. Affect this with [`Start()`](#start) and [`Stop()`](#stop).

##### `ActualTemperature`
The current temperature of the water.

##### `DesiredTemperature`
The target of the water temperature. Affect this with [`SetDesiredTemperature(Temperature)`](#setdesiredtemperaturetemperature).

##### `SetDesiredTemperature(Temperature)`
Change the set point of the water temperature. Reflected in [`DesiredTemperature`](#desiredtemperature).

##### `Start()`
Begin cooking. Reflected in [`IsRunning`](#isrunning). Stop with [`Stop()`](#stop).

##### `Stop()`
Stop cooking. Reflected in [`IsRunning`](#isrunning).

##### `StartTimer(TimeSpan)`
Start a countdown timer that will stop cooking after the given duration elapses. After calling this method, call [`Start()`](#start) to begin cooking for this duration. Can be cancelled with [`StopTimer()`](#stoptimer).

##### `StopTimer()`
Cancels any existing countdown timer that was previously started with [`StartTimer(TimeSpan)`](#starttimertimespan).

## Acknowledgements
- [**Luke Ma**](https://github.com/plluke) for generously giving me an Anova Precision Cooker sous vide as a Christmas gift in 2020.
