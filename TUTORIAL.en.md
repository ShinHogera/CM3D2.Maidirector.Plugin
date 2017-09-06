# Tutorial
This tutorial should (hopefully) present an overview of the various features available, and give some sense of an editing workflow.

First, start Photo Mode and open the timeline window by pressing `M` (with the default keybindings).

## Animating the camera
Most animations will probably animate the camera at some point, so let's start by manipulating the camera. To manipulate any object, including the camera, the corresponding track has to be added first.

To add a new track, press the `Add Track` button.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/1.png)

A window will appear querying for the track type. The default is `Camera`, so press `OK` to add the track.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/2.png)

When a track is created, a new clip will be automatically inserted into the created track. Clips contain the changes in parameters that happen over time. These can be represented as curves (from the side) or keyframes (from the top down).

The window with the lines below the timeline displays each curve. There is one curve for each individual scalar component to change. For example, the camera track will have curves for the X, Y, and Z coordinates of the camera's position.

To change the parameters, we must insert a keyframe. Keyframes represent the points in time that start and end a transition between two values. By default, there is already a single keyframe inserted at the beginning of the track for you at the left. Try dragging the topmost keyframe and see how the view responds.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut1.gif)

This will rotate the camera. However, there isn't any animation yet because there is only one keyframe. To transition between two states of the camera, we need to insert another keyframe later on in the clip. To insert a keyframe, first select a keyframe on the track you want to edit, then press the `Insert Keyframe` button. The button should change to the color of the curve. Then, click in the curve window at the place you want to insert the keyframe.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut2.gif)

There is now a curve between the two points. Drag the seeker at the top of the timeline window next to the curve in the track view, and press the play button (or press Space, with the default keybindings).

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut3.gif)

We've now animated the camera between the two values. This is the main way to animate things with Maidirector, and you'll be doing it often. You can combine multiple values for interesting results:

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut4.gif)

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut5.gif)

The camera in CM3D2 is not a stock Unity camera but an interface to a camera with an orbit point. This means the actual camera position is the "target position", and it is possible to orbit around this position. You can decrease the `Distance` parameter of the curve track to move closer to the target point, and orbit around it using the `Orbit X` and `Orbit Y` parameters.

## Advanced camera motion
Sometimes you might be able to move the camera to where you want it, but find that editing the curves is difficult. In this case, you can create keyframes for the current camera position at a specific time.

First, disable the camera track by pressing the `E` button next to the track. This will stop it from automatically moving.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut6.gif)

Next, move the seeker to the time you want the transition to end at.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut7.gif)

Now move the camera where you want it.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/3.png)

Finally, press the `K` button next to the track to insert the keyframes.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut8.gif)

The curves will automatically update with the camera position we've provided.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/4.png)

Now re-enable the track, press the stop button to reset the seeker, and test out your animation:

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut11.gif)

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut10.gif)

**Tip**: To capture the whole screen without the UI, press the `Delete` key to toggle it off and on.

## Animating objects
You can also control objects in the scene, like doors. First, you might want to delete any tracks you've previously created. To do so, click the `-` next to the track you want to delete.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut12.gif)

Load the `Entrance` background and position the camera in front of the front doors.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/5.png)

Now, press `Add Track` to add a new track. This time, change the `Type` to `Object`. More options will appear.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/6.png)

The objects are organized into categories depending on their type. For example, all objects in the background will appear in the `Background` category, which is the default. The objects for the main doors in this case are `MainDoorL` and `MainDoorR`. Select either one from the `Object` dropdown:

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/7.png)

When you want to change the position of an object, you'll want to use its `Transform` component. This is the default, so just press `OK`.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/8.png)

The new track will be created, but there are no curves, and the clip isn't selectable. To control an object, you have to specify what _properties_ of the object you want to change. To do this, press the `+` button next to the track and select the property you want. To open the door, we would want the `eulerAngles` property:

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut14.gif)

Now the curves appear, and you can change the door's rotation just as before:

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut15.gif)

## Animating maids
You can play one of the default animations on maids. To do so, create a new track, and select the `Maid Animation` category.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/9.png)

Select the maid and animation you want, and press `OK`.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut16.gif)

The animation will play for the duration of the track's clip. To change when the animation starts, you can drag the clip to a new location.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut19.gif)

To change the length of the clip, also changing its speed, change the drag mode to `Resize`:

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/10.png)

Then, drag the clip to resize it.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut17.gif)

To go back to dragging, press the `Drag` toggle.

You can also duplicate the selected clip by pressing the `Copy Clip` button:

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut18.gif)

## Other help
If anything is still confusing, please let me know and I'll try to add more instructions.

![](https://github.com/ShinHogera/CM3D2.Maidirector.Plugin/raw/master/images/tut20.gif)
