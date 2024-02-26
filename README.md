# Spider Procedural Animation Project

## Overview
This project implements a procedural animation system for a spider character in Unity.

## Features
- **Procedural Leg Movement**: Automatic calculation and adjustment of each leg's position to simulate walking, based on the spider's velocity and the terrain underneath.
- **Dynamic Body Orientation**: Adjusts the spider's body orientation according to the positions of its legs and the surface normals of the ground.
- **Customizable Settings**: Allows tweaking of various parameters such as step size, animation smoothness, and leg padding through the Unity Inspector to fine-tune the animation.

## Configuration
### Leg Animation Settings
- `Step Trigger Distance`: The minimum distance a leg must move before initiating a step.
- `Animation Smoothness`: The number of frames over which to smooth the stepping animation.
- `Step Lift Height`: The height each leg lifts off the ground during a step.
- `Ground Detection Radius`: Radius of the sphere cast used to detect the ground beneath each leg.
- `Ground Detection Depth`: How far beneath the spider to cast rays for ground detection.
- `Leg Padding`: Additional spacing applied between the front and back legs to avoid collision.

### Body Orientation Settings
- `Adjust Orientation`: Whether to dynamically adjust the spider's body orientation based on leg positions and ground normal.

## Usage
1. Attach the `SpiderController.cs` to an Empty GameObject
2. Attach the `SpiderProceduralAnimation` script to your spiders body (child of GameObject with `SpiderController.cs`).
3. Assign the `Leg Target Transforms` in the Inspector by dragging the corresponding objects from your scene.
4. Configure the `Leg Animation Settings` and `Body Orientation Settings` as desired.
5. Put the Spider on the Ingnore Raycast Layer
6. Play your scene.

## Gizmos
For debugging and visual assistance, gizmos are provided to visualize the target positions of each leg and the direction of velocity. Ensure that Gizmos are enabled in the Unity Inspector to view these.

### Screenshots
![image](https://github.com/Tr0sh55/Advaned_Procedural_Animation/assets/47827386/74e78d09-a509-4b78-9e10-7c96aed369b9)
