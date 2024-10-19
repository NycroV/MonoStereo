## Custom Implementations
MonoStereo supports custom implementations for songs, sounds, filters, and outputs. To use them, you can have classes inherit from `ISongSource`, `ISoundEffectSource`, `AudioFilter`, and `IMonoStereoOutput`.
You will *need* to provide a couple methods for each, and some more customization is optionally available through virtual overrides.

#### `!!! IMPORTANT !!!`
Audio reading/processing is done through 32-bit IEEE floating-point samples. If you are unfamiliar with the inner-workings of audio reading, it is recommended to stick with MonoStereo's supplied implementations - but if you would like to learn, [NAudio](https://github.com/naudio/NAudio/tree/master) has some great references to study from.

### Songs/Sound Effects
Song and sound sources use nearly identical code structure, but are separate to indicate how a source should be used.
For both sources, the main thing you will need to implement is a `Read(float[] buffer, int offset, int count)` method, which reads audio from your source into the buffer. However, there are numerous other methods available for implementation.

To use the custom sources:
```cs
public class MyCustomSongSource : ISongSource
{ /*...*/ }

public class MyCustomSoundEffectSource : ISoundEffectSource
{ /*...*/ }

Song song = Song.Create(new MyCustomSongSource());
SoundEffect sound = SoundEffect.Create(new MyCustomSoundEffectSource());
```

### Filters
To create a custom filter, implement a class that inherits from `AudioFilter`. This class does not have any *required* methods, and if you leave the class empty, it will be a filter with no effects.
You can override `ModifyRead()` to change the way that reading of the underlying source is handled, or `PostProcess()` to apply effects to audio after it has been read into memory. 

Within both your `ModifyRead()` and `PostProcess()` calls, your filter will access to a property titled `Source`, which represents the base audio source the filter is being applied to.
You will also have access to a property titled `Provider`, which is the filter that will be applied directly before the current one. Inside your `ModifyRead()` method, if you need to read the source, it is recommended to simply call `base.ModifyRead()`, but if you would prefer to directly call the read yourself, make sure you use `Provider` and not `Source` - using `Source` will cause an infinite loop as the filter tries to read itself over and over.

It is important to note that your filter has the ability to be applied to multiple sources. If you need to cache sample data to implement your filter, it is recommended to create a `Dictionary<MonoStereoProvider, YourFilterData>`, and override the `Apply()` and `Unapply()` methods to create/remove entries for each audio source. A filter can only be applied to a given source once, so no need to worry about duplicate dictionary entries.

```cs
public class MyCustomFilter : AudioFilter
{ /*...*/ }

MyCustomAudioFilter filter = new();

song.AddFilter(filter);
song.RemoveFilter(filter);
```

### Outputs
To create a custom audio output implementation, you can have a class implement the `IMonoStereoOutput` interface.

Within this interface, you will need to implement a total of 4 methods, and one property:
- `Init(AudioMixer waveProvider)` - This is called when the audio engine is initialized. `waveProvider` is the engine's master mixer, from which you can call `Read()` to gather audio data.
- `Play()` - Called when the audio engine starts. You should begin reading from the master mixer and playing to the output here.
- `Update()` - A method that is called once after every successive update chain in the audio engine. This is typically called very rapidly. You can update your output here.
- `Dispose()` - Called when the audio engine is shutting down. Dispose your resources here.
- `DesiredLatency { get; }` - A property that represents the desired latency for your output. It is up to you to make sure that this value accurately reflects your output's real latency.

After implementing all of the above, all you need to do is pass your custom output to the engine on startup!
```cs
MyCustomOutput output = new();
AudioManager.InitializeCustomOutput(output);
```