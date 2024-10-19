# MonoStereo
![Icon](https://github.com/user-attachments/assets/2005793d-6dfc-4367-b7ed-35615a580188)

[![nuget](https://badgen.net/nuget/v/MonoStereo?icon=nuget)](https://www.nuget.org/packages/MonoStereo)

[MonoStereo](https://github.com/NycroV/MonoStereo/tree/master) is an audio engine built for [MonoGame](https://github.com/MonoGame/MonoGame) using the [NAudio](https://github.com/naudio/NAudio/tree/master) audio framework.
> Although it was originally designed solely for MonoGame, it also contains a [Slim](https://github.com/NycroV/MonoStereo/tree/slim) variant that is standalone and can be used in any C#-based project - which contains all the same features!.

MonoGame's included audio support is lackluster, leaving many users to find audio implementation through other projects like [FMOD](https://github.com/fmod). MonoStereo aims to be a completely free, open-source, entirely C# native audio engine, built *specifically* with MonoGame projects in mind.

With included support for the Content Pipeline, dynamic filter application, multi-threading safety, and the ability to supply your own custom audio sources/filters where MonoStereo's provided implementations don't meet your needs, you'll have everything you need to really turn your game's audio quality up a notch.
## Features

- Entirely C# native
- Cross platform
- MonoGame Content Pipeline integration
- Default audio looping support from audio metadata with `LOOPSTART` and `LOOPEND`/`LOOPLENGTH` tags
- Dynamic audio filtering with 9 built-in filters
- Custom audio source support
- Direct IEEE (or PCM) sample access
- Support for changing audio output device

## Installation

MonoStereo (and MonoStereo.Slim) are available as packages on Nuget. To install, simply add one as a package reference through your project's package manager.

To use MonoStereo's Pipeline integration within MonoGame projects, you'll need to add a reference to a built `MonoStereo.dll`. The easiest way to go about this is to build your project once after MonoStereo has been installed - a compiled `MonoStereo.dll` should appear in your project's output directory. Reference this file and you should be able to use MonoStereo's custom audio importers and processors.