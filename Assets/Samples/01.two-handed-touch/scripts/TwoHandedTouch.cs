using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

/// <summary>
/// This class is attached to the HandInputController game object. It detects which of the three shapes are being touched using both hands simultaneously.
/// </summary>
public class TwoHandedTouch : MonoBehaviour
{
    public GameObject Cube;
    public GameObject Sphere;
    public GameObject Cone;

    private MLHand RightHand
    {
        get
        {
            return MLHands.Right;
        }
    }

    private MLHand LeftHand
    {
        get
        {
            return MLHands.Left;
        }
    }

    private readonly List<GameObject> Shapes = new List<GameObject> { };
    private readonly List<Touch> Touches = new List<Touch> { };
    private const float Distance = 0.15f; // This is how close the keypoint at the tip of your index finger needs to be from the game object to consider it a touch.

    /// <summary>
    /// Starts handtracking and adds the three basic shapes to a list of shapes when this game object is enabled.
    /// </summary>
    void Start()
    {
        MLResult result = MLHands.Start();
        if (!result.IsOk)
        {
            Debug.LogErrorFormat("Error: HandTrackingVisualizer failed starting MLHands, disabling script. Reason: {0}", result);
            enabled = false;
            return;
        }

        Shapes.Add(Cube);
        Shapes.Add(Sphere);
        Shapes.Add(Cone);
    }

    /// <summary>
    /// Evaluates a potential list of touches from the previous frame to see if they are still valid, removing them if they are not. New touches are added to the list.
    /// </summary>
    void Update()
    {
        // Check to see if any shapes which were touched in the previous frame by the right index finger are still being touched in the current frame.
        if (Touches.Exists(t => t.Hand == "Right"))
        {
            Touch touch = Touches.Find(t => t.Hand == "Right");

            if (RightHand.IsVisible)
            {
                // If the shape that was being touched by the right index finger is no longer being touched, remove the touch.
                if (!StillValid(touch, RightHand.Index.Tip.Position))
                {
                    Touches.Remove(touch);

                    // If the left index finger isn't touching the same shape, reset the color to white.
                    if (!Touches.Exists(t => t.Hand == "Left" && t.Shape == touch.Shape))
                    {
                        SetColor(touch.Shape, Color.white);
                    }
                }
            }
            else
            {
                SetColor(touch.Shape, Color.white);
                Touches.Remove(touch);
            }

        }

        // Check to see if any shapes which were touched in the previous frame by the left index finger are still being touched in the current frame.
        if (Touches.Exists(t => t.Hand == "Left"))
        {
            Touch touch = Touches.Find(t => t.Hand == "Left");

            if (LeftHand.IsVisible)
            {
                // If the shape that was being touched by the left index finger is no longer being touched, remove the touch.
                if (!StillValid(touch, LeftHand.Index.Tip.Position))
                {
                    Touches.Remove(touch);

                    // If the right index finger isn't touching the same shape, reset the color to white.
                    if (!Touches.Exists(t => t.Hand == "Right" && t.Shape == touch.Shape))
                    {
                        SetColor(touch.Shape, Color.white);
                    }
                }
            }
            else
            {
                SetColor(touch.Shape, Color.white);
                Touches.Remove(touch);
            }

        }

        // When the right hand is visible and was not touching a shape in the previous frame, check to see if it is currently touching a shape.
        if (RightHand.IsVisible && !Touches.Exists(t => t.Hand == "Right"))
        {
            Ray ray = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(RightHand.Index.Tip.Position));

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                // Check to see if the ray cast from the camera to the keypoint at the tip of the right index finger has hit one of the three shapes.
                if (Shapes.Exists(s => s.tag == hit.transform.tag && Vector3.Distance(s.transform.position, RightHand.Index.Tip.Position) <= Distance))
                {
                    Touch touch = new Touch
                    {
                        Hand = "Right",
                        Shape = Shapes.Find(s => s.tag == hit.transform.tag)
                    };

                    Touches.Add(touch);
                    SetColor(touch.Shape, Color.cyan);
                }
            }
        }

        // When the left hand is visible and was not touching a shape in the previous frame, check to see if it is currently touching a shape.
        if (LeftHand.IsVisible && !Touches.Exists(t => t.Hand == "Left"))
        {
            Ray ray = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(LeftHand.Index.Tip.Position));

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                // Check to see if the ray cast from the camera to the keypoint at the tip of the left index finger has hit one of the three shapes.
                if (Shapes.Exists(s => s.tag == hit.transform.tag && Vector3.Distance(s.transform.position, LeftHand.Index.Tip.Position) <= Distance))
                {
                    Touch touch = new Touch
                    {
                        Hand = "Left",
                        Shape = Shapes.Find(s => s.tag == hit.transform.tag)
                    };

                    Touches.Add(touch);
                    SetColor(touch.Shape, Color.cyan);
                }
            }
        }

        // Reset the shapes and touches if neither hand is visible.
        if (!LeftHand.IsVisible && !RightHand.IsVisible)
        {
            foreach (GameObject shape in Shapes)
            {
                SetColor(shape, Color.white);
            }

            Touches.Clear();
        }
    }

    /// <summary>
    /// Checks to see if the given touch is still valid (i.e. the hand specified in the touch is still touching the shape specified in the touch).
    /// </summary>
    /// <param name="touch">An instance of a custom class containing a hand and the game object that hand's index finger was touching.</param>
    /// <param name="keypoint">The vector3 position of the tip of either the right or left index finger.</param>
    private bool StillValid(Touch touch, Vector3 keypoint)
    {
        Ray ray = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(keypoint));

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            if (hit.transform.tag == touch.Shape.tag && Vector3.Distance(keypoint, touch.Shape.transform.position) <= Distance)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Changes the material color applied to the specified shape.
    /// </summary>
    /// <param name="shape">The game object who's color should be changed.</param>
    /// <param name="color">The color to change the material color on the game object to.</param>
    private void SetColor(GameObject shape, Color color)
    {
        shape.GetComponent<MeshRenderer>().material.color = color;
    }

    /// <summary>
    /// Stops hand tracking when this game object no longer exists.
    /// </summary>
    void OnDestroy()
    {
        if (MLHands.IsStarted)
        {
            MLHands.Stop();
        }
    }

    /// <summary>
    /// A custom class used to store touch information including the game object that was touched and the hand who's index finger was touching it.
    /// </summary>
    internal class Touch
    {
        public GameObject Shape { get; set; }
        public string Hand { get; set; }
    }

}
