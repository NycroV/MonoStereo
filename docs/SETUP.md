## Setup
To begin using MonoStereo, first initialize the audio engine in your game's startup code. This can be done anywhere, but it's recommended to place inside of your `Game`'s `Initialize()` method.

Use the `AudioManager.Initialize()` method to start the audio engine. You should supply this method with a `Func<bool>` that lets the audio engine know when to shut down, as it runs on a separate thread to improve performance.

```cs
protected override void Initialize()
{
    base.Initialize();

    // Your other initialization code here

    // `isRunning` will be false when the game stops.
    AudioManager.Initialize(() => !isRunning);
}
```

You also have access to a few extra variables on startup, namely `masterVolume`, `musicVolume`, and `soundEffectVolume` - as well as `latency`, `deviceNumber`, and `bufferCount`.

- `masterVolume`, `musicVolume`, and `soundEffectVolume` are pretty self explanatory. These values should be `float`s that range from 0-1, with 1 being max volume, and 0 being mute. If you want to change the music, sound, or master volumes later, they are available with the properties `AudioManager.MasterVolume`, `AudioManager.MusicVolume`, and `AudioManager.SoundEffectVolume`.
- `latency` is the desired latency, in milliseconds, before audio reaches the output device. Increasing this can create a slight delay in audio playback, but helps to reduce choppy audio when lots of post-processing effects are applied. The default 75ms is typically fine, but you may find yourself increasing or decreasing this value depending on your use-case.
- `deviceNumber` is the index of the output device you want to play to. -1 uses the system default.
- `bufferCount` is the number of buffers that you want to split your latency among. Increasing this can allow for lower latencies, but is more likely to cause clipping with certain effects. It is highly recommended to keep this value at the default 8.

By default, this output should have everything you need. However, if you want a different way to output audio - for example, maybe you want to broadcast it to a remote source - you can create a custom class that implements the `IMonoStereoOutput` interface.
From there, you can use the following code:

```cs
protected override void Initialize()
{
    base.Initialize();

    // Your other initialization code here

    var output = new MyCustomOutput();

    // `isRunning` will be false when the game stops.
    AudioManager.InitializeCustomOutput(output, () => !isRunning);
}
```

This method also takes in parameters for the master, music, and sound mixer volume levels.
> For more info on how to create custom outputs, see [Custom Sources](https://github.com/NycroV/MonoStereo/blob/master/docs/CUSTOM_SOURCES.md)

Once the audio engine has been started, it's a good idea to incorporate `AudioManager.ThrowIfErrored()` somewhere into your game's update loop. This will forward any errors that might have been thrown from the audio thread to your main thread, allowing for easier debugging and possible deadlock prevention.

Now that you've set up your audio engine, you're ready to play some audio! See the [Playback](https://github.com/NycroV/MonoStereo/blob/master/docs/PLAYBACK.md) documentation.
