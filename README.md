👨🏻‍🍳 SousVide
===

[![Nuget](https://img.shields.io/nuget/v/SousVide?logo=nuget)](https://www.nuget.org/packages/SousVide/)

*Bluetooth client library and protocol specification for the Anova Precision Cooker sous vide.*

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3,4" bullets="1.,-,-,-" -->

1. [Background](#background)
1. [Prerequisites](#prerequisites)
1. [Usage](#usage)
    - [Bluetooth Pairing](#bluetooth-pairing)
    - [Command Line Tool](#command-line-tool)
    - [.NET Library](#net-library)
        - [Getting Started](#getting-started)
        - [Application Programming Interface](#application-programming-interface)
1. [Communication Protocol Specification](#communication-protocol-specification)
1. [Bluetooth Inspection](#bluetooth-inspection)
1. [Acknowledgements](#acknowledgements)

<!-- /MarkdownTOC -->

## Background

The original [Anova Precision Cooker 1.0](https://www.amazon.com/Anova-Culinary-Precision-Bluetooth-Included/dp/B00UKPBXM4/) is an 800 watt sous vide which was released in 2014. You can control it with either physical on-device inputs or a [phone app](https://play.google.com/store/apps/details?id=com.anovaculinary.android) over Bluetooth Low Energy. Unlike later models, it does not have a Wi-Fi tranceiver. It's a very nice device and it works well.

Unfortunately, Anova [decided to charge users who create an account after 2024-08-21 $2/month or $10/year (USD) to control their sous vides from the phone app](https://www.theverge.com/2024/8/19/24223878/anova-sous-vide-kitchen-app-subscription). They also [decided to remove the existing Bluetooth connectivity from their app on 2025-09-28](https://arstechnica.com/gadgets/2024/08/smart-sous-vide-cooker-to-start-charging-2-month-for-10-year-old-companion-app/), so the only way to use these devices will be with the physical controls. This library and protocol specification were inspired by a reader's objection to this bait-and-switch cash grab:
> Some long-time users have pleaded with the company to think of alternative solutions. For example, a commenter called David, who claimed to own three Anova products, asked if the company could "open source the communication protocols and allow the community to take over."
> 
> "I suspect there is a strong overlap between people who own sous vides and developers (me for a start)," David said.

To be clear, **the ability to remotely control a sous vide is worthless**. There is no advantage to starting, monitoring, or stopping cooking with your phone:
- It's far clumsier and slower than the physical dial and button located on the sous vide itself.
- You're going to be near the device when you start it anyway, so you don't need to start it from far away.
- The cooking with the phone app is not better than with the manual inputs because the only relevant factors are the target water temperature and the signal to begin cooking.
- The timer is useless because the entire point of sous vide is that it can run forever, so if you want to know when two hours have elapsed, set a timer on your phone or oven.
- If you want to sous vide as quickly as possible, the only correct solution is to use a continuous thermometer like the [Thermoworks Dot](https://www.thermoworks.com/dot/).
- Being able to transfer online "recipes" is a waste of time because they are so simplistic that they just set a useless timer and a target water temperature that you could have easily set yourself, either by looking it up on your [handy temperature guide](https://www.thermoworks.com/content/pdf/chef_recommended_temps.pdf) or a quick web search.
- There is no reason to check the water temperature remotely, because it will always be at the target temperature thanks to the sous vide running.
- There is no reason to change the water temperature set point remotely, because the point of sous vide is using a constant temperature for the entire cooking duration.

If you *still* want to control your Anova Precision Cooker over Bluetooth after 2025-09-28, you can try the following techniques until one works.
1. Install an [older version of the app](https://apkpure.com/anova-culinary/com.anovaculinary.android/download/3.5.1) that still has the Bluetooth functionality.
1. Use the sample app in this repository.
1. Write your own .NET program using the [client library in this repository](https://www.nuget.org/packages/SousVide/).
1. Write your own program in a programming language of your choice by following the [protocol specification](https://github.com/Aldaviva/SousVide/wiki/Communication-Protocol) in this repository.
1. [Inspect Bluetooth LE traffic](https://github.com/Aldaviva/SousVide/wiki/Bluetooth-Low-Energy-Interception) between your phone and sous vide to understand and implement the GATT communication protocol yourself (it's misguided but simple, just RPC-style string writes and response callbacks used to read and write values and invoke functions).

## Prerequisites
- Anova sous vide
    - Tested on a [Precision Cooker 1.0 with Bluetooth](https://www.amazon.com/Anova-Culinary-Precision-Bluetooth-Included/dp/B00UKPBXM4/) (2014)
- Bluetooth controller
    - Tested on an [Asus USB-BT400](https://www.asus.com/us/networking-iot-servers/adapters/all-series/usbbt400/) and [Intel AX211](https://ark.intel.com/content/www/us/en/ark/products/204837/intel-wi-fi-6e-ax211-gig.html)
- .NET Framework 4.6.2, 6.0, or later

## Usage
### Bluetooth Pairing
1. Plug in the sous vide.
1. Press its Bluetooth button to enter pairing mode. It will turn blue and start blinking.
1. Scan for the sous vide in the Bluetooth settings of your operating system.
    - Windows 10
        1. Go to Settings › Devices › Bluetooth & other devices > Add Bluetooth or other device › Bluetooth.
        1. Select `Anova`.
    - Windows 11
        1. Go to Settings › Bluetooth & devices › Devices.
        1. Set Device settings › Bluetooth devices discovery to **Advanced**, otherwise the device will be hidden.
        1. Select Add device › Bluetooth.
        1. Select `Anova`.
1. Enter the PIN `0000`.

### Command Line Tool
1. Download the `SousVideCtl` ZIP file for your operating system and CPU architecture from the [latest release](https://github.com/Aldaviva/SousVide/releases/latest).
1. Extract the ZIP file.
1. On Linux- and Unix-based operating systems, set the executable bit using `chmod +x sousvidectl`.

TODO

### .NET Library
#### Getting Started
1. In your .NET project, add a depenency on the NuGet package [`SousVide`](https://www.nuget.org/packages/SousVide/).
    ```sh
    dotnet add package SousVide
    ```
1. When targeting Windows, add a Windows 10 target framework moniker to the `<TargetFrameworks>` in your `.csproj` project file, otherwise it will use the Linux implementation if you only target an OS-agnostic TFM like `net8.0`.
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

##### `ISousVide.DeviceId`
The unique ID of this Bluetooth device. Can be persisted and passed to `AnovaPrecisionCooker.Create(string?)` later to reconnect to the exact same device, in case there are multiple paired Precision Cookers.

##### `ISousVide.IsRunning`
`true` if the sous vide is cooking, or `false` otherwise. Change this with [`Start()`](#isousvidestart) and [`Stop()`](#isousvidestop).

##### `ISousVide.ActualTemperature`
The current temperature of the water.

##### `ISousVide.DesiredTemperature`
The target of the water temperature. Change this with [`SetDesiredTemperature(Temperature)`](#isousvidesetdesiredtemperaturetemperature).

##### `ISousVide.SetDesiredTemperature(Temperature)`
Change the set point of the water temperature. Changes [`DesiredTemperature`](#isousvidedesiredtemperature).

##### `ISousVide.Start()`
Begin cooking. Changes [`IsRunning`](#isousvideisrunning). Stop with [`Stop()`](#isousvidestop).

##### `ISousVide.Stop()`
Stop cooking. Changes [`IsRunning`](#isousvideisrunning).

##### `ISousVide.StartTimer(TimeSpan)`
Start a countdown timer that will stop cooking after the given duration elapses. After calling this method, call [`Start()`](#isousvidestart) to begin cooking for this duration. Can be cancelled with [`StopTimer()`](#isousvidestoptimer).

##### `ISousVide.StopTimer()`
Cancels any existing countdown timer that was previously started with [`StartTimer(TimeSpan)`](#isousvidestarttimertimespan).

## Communication Protocol Specification
See [Communication Protocol](https://github.com/Aldaviva/SousVide/wiki/Communication-Protocol).

## Bluetooth Inspection
See [Bluetooth Low Energy Interception](https://github.com/Aldaviva/SousVide/wiki/Bluetooth-Low-Energy-Interception).

## Acknowledgements
- [**Luke Ma**](https://github.com/plluke) for generously giving me an Anova Precision Cooker as a Christmas present in 2020.