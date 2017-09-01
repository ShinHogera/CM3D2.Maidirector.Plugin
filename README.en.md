# CM3D2.Maidirector.Plugin
This is an animation tool for CM3D2. Play animations on maids and animate the parameters of the camera, maid faces, and any other object in the scene.

![Screenshot](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/screenshot.png)

## Installation
* Place `CM3D2.Maidirector.Plugin.dll` into your `UnityInjector` directory.
* Place the contents of the `Config` folder into your `UnityInjector/Config` directory.

## Building
Place `Assembly-CSharp.dll`, `UnityEngine.dll`, `ExIni.dll`, and `UnityInjector.dll` in a `References` directory one level up from the repo directory, then run `msbuild`.

## Usage
**A step-by-step tutorial is available [here](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/blob/master/TUTORIAL.en.md).**

Photo mode is recommended.

### Timeline View
Open the timeline window and press `Add Track` to create a track. The following track types are available:
- `Camera`: Control the camera.
- `Object`: Control the properties of any object in the scene.
- `Maid Animation`: Play an animation on a maid. The animation speed can be changed by changing the size of the curve clip.
- `Maid Face`: Control the parameters of maids' faces.

These are the actions you can take using the buttons next to each track:
- `[E]nable`: Enable/disable playback of the track's parameters.
- `[K]eyframe`: Insert keyframes on this track using the current values of the track's target object at the current time. One way to use this is to place the camera in one place, insert a group of keyframes, then increment the time, move the camera to another location and insert another group of keyframes. This way you can interpolate between two states of the camera.
- `[C]lip`: Insert a new clip.
- `[-]`: Deletes the track.
- `[+]`: In Object tracks, adds a property or field to modulate.

Select a clip by clicking on it or dragging it and it will be displayed in the curve view.

To resize tracks, press the `Resize` toggle and drag the clip you want to resize. To go back to dragging, press the `Drag` toggle.

### Curve View
Here you can manipulate keyframes bound to the parameters of the selected clip by dragging the keyframe handles. Explanations of the other controls:

- `+`/`-` - Zoom in and out.
- `▲`/`▼` - Pan the view up and down.
- `◀`/`▶` - Select the previous/next curve.
- `Center` - Fit the selected curve to the curve window.
- `Fit All` - Fit all curves to the curve window.
- `Insert Key` - Starts insertion of a keyframe on the currently selected curve. To insert the keyframe, click the location in the curve window where you want the keyframe to be inserted in the selected curve.
- `Delete Key` - Deletes the current (most recently selected) keyframe.
- `Tangent Mode` - Changes the behavior of the selected keyframe's tangents.
- `Broken` - Sets whether the selected keyframe's tangents should form a straight line or be controlled separately.
- `Wrap Mode` - Changes the behavior of the curve past the first/last keyframes.

The panel for toggling visibility of keyframes can be accessed with the `Toggle Visible` button. This is useful for accessing a specific curve when keyframes from other curves overlap.

### Keyframe View
Toggle the keyframe view by pressing the `Keyframe` button. This provides a top-down overview of all keyframes, similar to MikuMikuDance. Using this view, more precise values for keyframes can be input.

### Keybindings
These are the default keybindings, but they can be edited in the `UnityInjector/Config/Maidirector.ini` config file.

| Key     | Function                |
|---------|-------------------------|
| M       | Open Timeline Window    |
| Space   | Play/Pause Take         |
| S       | Stop Take               |
| Delete  | Hide UI                 |

## Known Issues
- Keyframe curves may oscillate wildly if they're too close together. Set the keyframe tangent modes to `Linear` to prevent this.
- Clips may jump across the track if they're overlapped during dragging/resizing.
- The seeker may go off the side of the screen. It can be reset by pressing the stop button twice.

## TODO
- Saving/loading
- MMD-like IK animation
- Lipsync support
- Ability to crop clips

## Contributing
Feel free to contribute if you have any bug reports/suggestions.
