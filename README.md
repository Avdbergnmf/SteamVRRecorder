# SteamVRRecorder
Directly record SteamVR data from Unity using separate thread.

## Credits
Much of this was taken directly from [Nicolas Wenk](https://github.com/nicolaswenk).

## How to use
Import all scripts to the unity projects. SteamVR should also be imported.

### Trackerconfigurationloader  
Create `Assets/StreamingAssets/tracker_config.txt`, and add `trackername;tracker-ID` per line, example:
```
tracker1;LHR-EE216FB0
tracker2;LHR-68FB5B80
tracker3;LHR-BBCC144D
```

Set up trackers
- Create gameobject for your tracker.
- Add tracker configurator loader component.
- set the name of the tracker you want to record, example: `tracker2`

### Record

- drag Recorder prefab into the scene. 
- Add the Tracker Configuration Loader to the list to record. 
- Recording starts on awake.

### Replay

To replay, drag Replayer prefab into the scene. (todo)

