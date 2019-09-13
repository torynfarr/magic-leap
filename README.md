# Magic Leap
This repository contains various sample Unity projects designed for the Magic Leap One spatial computing device.
<br />
<br />

## 01.[Two Handed Touch](https://github.com/torynfarr/magic-leap/tree/master/Samples/01.two-handed-touch)

This sample uses hand tracking and demonstrates how to detect when the keypoint on the tip of your index finger on either hand touches a game object. 

- When a game object has been touched, the color will change to cyan.

- When you're no longer touching that object, the color will be reset to white. 

- If the index fingers on both hands are touching the same object, the color won't be set back to white until both fingers are no longer touching that object.

- Hand Meshing is included to facilitate occlusion. It's optional and not used to detect when an object has been touched.
<br />
<img src="https://github.com/torynfarr/magic-leap/blob/master/docs/images/twohandedtouch.gif" width="350">
<br />

## 02.[Pinch and Drag](https://github.com/torynfarr/magic-leap/tree/master/Samples/02.pinch-and-drag)
 
This sample uses hand tracking and demonstrates one approach to picking up a game object and moving it around by holding / pinching it between the tips of your index finger and thumb on either hand.

- When the tips of your index finger and thumb are both touching the same sphere for a set duration of time (half a second) and your hand is in the C, pinch, or OK keypose, the sphere will be picked up.

- As your move the hand holding the sphere, the spheres position will update to the center of the gap between the tips of your index finger and thumb.

- If you spread your fingers apart or if your hand is no longer visible, the sphere will be dropped

- Only one hand can be used at at a time. This is to prevent the free hand from obscuring the keypoints on the hand being used to pinch a sphere.
<img src="https://github.com/torynfarr/magic-leap/blob/master/docs/images/pinchanddrag.gif" width="350">
<br />

## Additional Information

- These samples were created using Unity version 2019.2.5f1
- The Magic Leap Unity package is version 0.22.0