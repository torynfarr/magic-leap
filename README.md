# Magic Leap
This repository contains various sample Unity projects designed for the Magic Leap One spatial computing device.

## [Two Handed Touch](https://github.com/torynfarr/magic-leap/TwoHandedTouch) 
This sample uses hand tracking and demonstrates how to detect when the keypoint on the tip of your index finger on either hand touches a game object. 

- When a game object has been touched, the color will change to cyan.

- When you're no longer touching that object, the color will be reset to white. 

- If the index fingers on both hands are touching the same object, the color won't be set back to white until both fingers are no longer touching that object.

- Hand Meshing is included to facilitate occlusion. It's optional and not used to detect when an object has been touched.

<img src="https://github.com/torynfarr/magic-leap/blob/master/docs/images/twohandedtouch.gif" width="350">

### Additional Information
- This sample was created using Unity version 2019.2.2f1
- The Magic Leap Unity package is version 0.22.0