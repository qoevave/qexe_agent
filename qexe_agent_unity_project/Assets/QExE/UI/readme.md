*Unity Version 2021.3.13f* 
*HDRP Version 12.1.7*
*XR Interaction Toolkit Version 2.3.0*

# UI - Dependencies and General Information

## Missing Dependencies

Scripts:
- TrackedDeviceGraphicRaycaster.cs
- OSCPacket.cs
- TestHooks.cs

Packages:
-TextMeshPro

## General Information

How the UI functions:
- All UI Elements should be under the TestUI gameObject
- TestUI should have the script "UIManager.cs" attached
- Every UI Prefab acts as a canvas and needs the following scripts to function:
	- UIColorManager.cs
	- TrackedDeviceGraphicRaycaster.cs
	- OSCPacket.cs
	- TestHooks.cs
- The Popup-Panel is controlled through animation
	- Text and Images can be altered without worry (except for their name)
	- If something new is added it has to be animated as well

How the UIManager.cs script functions:
This script needs to be attached to a parent object. 
The UI elements need to be children of this parent and have a UIColorManager.cs script attached.
Every customizable element needs to be assigned a tag which is used to give them their selected colour.
Every button, slider and image component needs to be kept on seperate gameObjects for the colours to be assigned properly.

How the SliderButton.cs script functions:
The corresponding slider has to be attached to the script. After that the buttons "On Click" can be assigned the corresponding script funcion "SliderButtons.ButtonUp" or "SliderButtons.ButtonDown"

