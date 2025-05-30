## Playback
> **Note for if you are using MonoGame:**<br/>
Before playing audio, you'll want to compile your sources to the correct MonoStereo format. Although this isn't *required*, it is having a standardized format for both songs and sound effects can help to improve performance in practice.
>
> First, add a reference to `MonoStereo.Pipeline.dll` in your `Content.mgcb` file (or whichever pipeline file you want to use). If you need access to this .dll, simply build your game once after adding MonoStereo (and MonoStereo.Pipeline) as a package reference, and it should appear in your output folder. From there, choose the MonoStereo Audio Importer for all audio you want to compile, and the corresponding song or sound effect processor, depending on how you plan to use the audio.

### Songs
Playing audio with MonoStereo is very simple. To play a song, use the following:
```cs
Song song = Song.Create("path/to/song");
song.Play();
```
If you have not compiled your songs using MonoStereo.Pipeline, you'll need to include the file extension as well.<br/>
If your files *have* been compiled by MonoStereo.Pipeline, you do not need to add `.xnb` to the end - MonoStereo will handle it for you.

This method creates a reader that directly reads from audio files straight into the output. This works fine, but in some cases, the expensive IO reading may be too slow to keep up with playback. In this event, MonoStereo provides a "buffered reader", which offloads the expensive IO operations to a background thread, queuing samples a configurable amount of seconds ahead for playback.

To implement buffered readers, use the following:
```cs
Song song = Song.CreateBuffered("path/to/song", secondsToHold); // Will read secondsToHold seconds ahead of the playback and queue the samples.
song.Play();
```
Buffered reading only reads the source file ahead of time. Effects and filters are still applied in real time, based off of your configured latency.
You may have noticed some extra overloads to these methods which take in custom song sources. For more info about custom sources, see the [Custom Sources](https://github.com/NycroV/MonoStereo/blob/master/docs/CUSTOM_SOURCES.md) documentation.

### Sound Effects
When playing sound effects, you have 2 options. For sounds that won't be played back very frequently, you can create and play them the same way you would with a song.
```cs
SoundEffect sound = SoundEffect.Create("path/to/sound");
sound.Play();
```
Alternatively, if a sound is going to be played back frequently, it may be better to cache the sound's data in memory for quicker and more efficient access.
```cs
// Both of these methods do the same thing - use whichever you think looks nicer.

// Option 1
CachedSoundEffect cachedSound = CachedSoundEffect.Create("path/to/sound");

// Option 2
CachedSoundEffect cachedSound = SoundEffect.Cache("path/to/sound");
```
Now that the sound is cached in memory, you can play it one of two ways:
```cs
// Option 1
cachedSound.PlayInstance();

// Option 2
SoundEffect sound = cachedSound.GetInstance(); // Alternatively: SoundEffect.Create(cachedSound);
sound.Play();
```
When you no longer need the cached sound effect, call `cachedSound.Dispose()` to dispose the object.

#### Note:
Whenever a song or sound effect completes its playback, it is automatically removed from the mixer. If you want to override this behavior, you can have a class inherit from either `Song` or `SoundEffect` and override the `RemoveInput()` method.

For both songs and sounds, you have access to the `Song` and `SoundEffect` instances that control playback for these objects. Looping support is integrated by default - just change the `IsLooped` property. Additionally, you can call `Pause()`, `Resume()`, and `Stop()` to control the playback state.

A couple interfaces with some extra capabilties are implemented by default in MonoStereo. If you want to check if a song or sound effect's source is seekable (and, subsequently, seek it), try casting `Song.Source` (or `SoundEffect.Source`) to an `ISeekable`, and use the `Position` property. If you want to check for looping tags, try casting to an `ILoopTags`. All of MonoStereo's integrated sources implement these interfaces by default.

To add filters to your audio, see the [Filters](https://github.com/NycroV/MonoStereo/blob/master/docs/FILTERS.md) documentation.
