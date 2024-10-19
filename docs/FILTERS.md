## Filters
Adding filters to your audio with MonoStereo is very easy. All you need to do is create a new filter instance, and add it to your audio with the `AddFilter()` method.
```cs
Song song = Song.Create("path/to/song");
song.Play();

// Whenever you want to add the filter...
PitchShiftFilter filter = new PitchShiftFilter(pitch);
song.AddFilter(filter);

// Whenever you want to remove the filter...
song.RemoveFilter(filter);
```
MonoStereo contains 9 built-in filters available to use:
- `HighPassFilter`
- `LowPassFilter`
- `PanFilter`
- `PitchShiftFilter`
- `PositionFilter`
- `ReverbFilter`
- `SpeedChangeFilter`
- `TempoChangeFilter`
- `VolumeFilter`

Additionally, filter instances are shareable across multiple audio instances. If you create one instance of a `PitchShiftFilter`, it can be applied to multiple audio sources (songs and sounds alike) - however, filters cannot be applied to the same source multiple times. Changing a property of the filter will carry that change over to every other audio source using the same filter.
```cs
SpeedChangeFilter speed = new(0.5f);

song1.AddFilter(speed);
song2.AddFilter(speed);

speed.Speed = 0.4f;
// Applies to both song1 and song2
```
If you want to apply a filter to every instance of a song or sound, rather than individually applying it to each one, apply it to the respective mixer:
```cs
AudioManager.MusicMixer.AddFilter(filter);
AudioManager.SoundMixer.AddFilter(filter);
AudioManager.MasterMixer.AddFilter(filter);
```
> Note: since filters are reference types, in order to remove them, you will need to keep track of your filter's object instance.

In order to implement custom audio filters, see the [Custom Sources](https://github.com/NycroV/MonoStereo/blob/master/docs/CUSTOM_SOURCES.md) documentation.
