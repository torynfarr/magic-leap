using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

/// <summary>
/// This class is attached to the HandInputController game object. It detects which of the three spheres are being touched and facilitates pinching and dragging the spheres around.
/// </summary>
public class PinchAndDrag : MonoBehaviour
{
    public GameObject Sphere1;
    public GameObject Sphere2;
    public GameObject Sphere3;
    public Text Status;

    private enum Phases { Selecting, Moving };
    private Phases Phase { get; set; }

    private string PreviousRaycastHitTag = string.Empty;
    private GameObject Highlighted;
    private GameObject Pinched;
    private LineRenderer PinchLine;
    private MLHandType HandInUse;
    private float StatusTextTimer = 0f;
    private readonly List<GameObject> Spheres = new List<GameObject> { };

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

    private const float Distance = 0.15f; // This is how close the keypoint at the tip of your index finger needs to be from the sphere to consider it a touch.
    private const float PinchDuration = 0.5f; // This is how long the pinch needs to be held before the sphere being pinch is picked up.
    private const float MaxPinchWidth = 0.045f; // This is the maximum distance between the tips of your index finger and thumb before a sphere is dropped.

    /// <summary>
    /// Starts handtracking, sets up the line renderer, and puts the spheres in a list of game objects.
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
        
        PinchLine = GameObject.FindGameObjectWithTag("Pinch Line").GetComponent<LineRenderer>();
        PinchLine.widthMultiplier = 0.0025f;

        Spheres.Add(Sphere1);
        Spheres.Add(Sphere2);
        Spheres.Add(Sphere3);
    }

    /// <summary>
    /// Calls either the selecting or moving phase methods each frame.
    /// </summary>
    void Update()
    {
        switch (Phase)
        {
            case Phases.Selecting:
                {
                    SelectingPhase();
                    return;
                }

            case Phases.Moving:
                {
                    MovingPhase();
                    return;
                }
        }
    }

    /// <summary>
    /// Handle input during the selecting phase. Called with each frame.
    /// </summary>
    private void SelectingPhase()
    {
        // Reset the status text timer and the status text itsef after three seconds (if the timer has been set/started).
        if (StatusTextTimer > 0f)
        {
            StatusTextTimer += Time.deltaTime;

            if (StatusTextTimer >= 3f)
            {
                StatusTextTimer = 0f;
                Status.text = "Touch a sphere with your index finger.";
                Status.text += "\n\r\n\rHold your hand up to reset the spheres.";
            }

        }

        // Unhighlight all spheres if both hands are visible.
        if (RightHand.IsVisible && LeftHand.IsVisible)
        {
            SetColor(Sphere1, Color.red);
            SetColor(Sphere2, Color.blue);
            SetColor(Sphere3, Color.green);
            Highlighted = null;
            PreviousRaycastHitTag = string.Empty;
            return;
        }

        Ray ray;

        if (RightHand.IsVisible)
        {
            // Reset the positions of the spheres if the open-hand key pose is detected.
            if (RightHand.KeyPose == MLHandKeyPose.OpenHand)
            {
                ResetSpheres();
                Status.text = "Spheres have been reset!";
                StatusTextTimer += Time.deltaTime;
                return;
            }

            ray = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(RightHand.Index.Tip.Position));

            Highlight(ray);

            if (Highlighted != null)
            {
                HandInUse = MLHandType.Right;

                // When the tips of the thumb and index finger are touching the same sphere, start a delayed pickup.
                if (Pinching(RightHand.Thumb.Tip.Position, RightHand.Index.Tip.Position))
                {
                    StatusTextTimer = 0f;
                    Status.text = $"Pinching...";
                    Invoke("DelayedPickup", PinchDuration);
                }
            }

            return;

        }

        if (LeftHand.IsVisible)
        {
            // Reset the positions of the spheres if the open-hand key pose is detected.
            if (LeftHand.KeyPose == MLHandKeyPose.OpenHand)
            {
                ResetSpheres();
                Status.text = "Spheres have been reset!";
                StatusTextTimer += Time.deltaTime;
                return;
            }

            ray = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(LeftHand.Index.Tip.Position));

            Highlight(ray);

            if (Highlighted != null)
            {
                HandInUse = MLHandType.Left;

                // When the tips of the thumb and index finger are touching the same sphere, start a delayed pickup.
                if (Pinching(LeftHand.Thumb.Tip.Position, LeftHand.Index.Tip.Position))
                {
                    StatusTextTimer = 0f;
                    Status.text = $"Pinching...";
                    Invoke("DelayedPickup", PinchDuration);
                }
            }

            return;
        }

    }

    /// <summary>
    /// Handle input during the moving phase. Called with each frame.
    /// </summary>
    private void MovingPhase()
    {
        switch (HandInUse)
        {
            case MLHandType.Right:
                {
                    // Skip this frame if the hand tracking is less than 95% confident.
                    if (RightHand.HandConfidence < 0.95f)
                    {
                        return;
                    }

                    PinchLine.SetPosition(0, RightHand.Index.Tip.Position);
                    PinchLine.SetPosition(1, RightHand.Thumb.Tip.Position);

                    // Drop the held sphere if the gap between the tips of the index finger and thumb is too wide or if the hand holding the sphere is no longer visible.
                    if (DropSphere())
                    {
                        Status.text = $"You let go of the {Pinched.tag.ToLower()} sphere!";
                        StatusTextTimer += Time.deltaTime;
                        Pinched = null;
                        Phase = Phases.Selecting;
                        break;
                    }

                    if (RightHand.IsVisible)
                    {

                        // Move the sphere to the center of the line renderer which is positioned between the tips of the right index finger and thumb.
                        if (Pinched != null)
                        {

                            // Only change the position of the sphere if your fingers have moved this frame.
                            if (Pinched.gameObject.transform.position != PinchLine.bounds.center)
                            {
                                Status.text = $"Moving the {Pinched.tag.ToLower()} sphere with your right hand.";
                                Status.text += "\n\r\n\rRelease the sphere to let go!";
                                Pinched.gameObject.transform.position = PinchLine.bounds.center;
                            }

                        }
                        else
                        {
                            Status.text = $"You let go of the {Pinched.tag.ToLower()} sphere!";
                            StatusTextTimer += Time.deltaTime;
                            Pinched = null;
                            Phase = Phases.Selecting;
                            break;
                        }

                    }

                    break;
                }

            case MLHandType.Left:
                {
                    // Skip this frame if the hand tracking is less than 95% confident.
                    if (LeftHand.HandConfidence < 0.95f)
                    {
                        return;
                    }

                    PinchLine.SetPosition(0, LeftHand.Index.Tip.Position);
                    PinchLine.SetPosition(1, LeftHand.Thumb.Tip.Position);

                    // Drop the held sphere if the gap between the tips of the index finger and thumb is too wide or if the hand holding the sphere is no longer visible.
                    if (DropSphere())
                    {
                        Status.text = $"You let go of the {Pinched.tag.ToLower()} sphere!";
                        StatusTextTimer += Time.deltaTime;
                        Pinched = null;
                        Phase = Phases.Selecting;
                        break;
                    }

                    if (LeftHand.IsVisible)
                    {

                        // Move the sphere to the center of the line renderer which is positioned between the tips of the left index finger and thumb.
                        if (Pinched != null)
                        {

                            // Only change the position of the sphere if your fingers have moved this frame.
                            if (Pinched.gameObject.transform.position != PinchLine.bounds.center)
                            {
                                Status.text = $"Moving the {Pinched.tag.ToLower()} sphere with your left hand.";
                                Status.text += "\n\r\n\rRelease the sphere to let go!";
                                Pinched.gameObject.transform.position = PinchLine.bounds.center;
                            }

                        }
                        else
                        {
                            Status.text = $"You let go of the {Pinched.tag.ToLower()} sphere!";
                            StatusTextTimer += Time.deltaTime;
                            Pinched = null;
                            Phase = Phases.Selecting;
                            break;
                        }

                    }

                    break;
                }
        }

    }

    /// <summary>
    /// Highlights the sphere hit by the provided ray.
    /// </summary>
    /// <param name="ray">A ray going from the camera to the point of input.</param>
    private void Highlight(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            float distance = 0.0f;
            string hand = string.Empty;

            if (RightHand.IsVisible)
            {
                hand = "right";
                distance = Vector3.Distance(RightHand.Index.Tip.Position, hit.transform.position);
            }

            if (LeftHand.IsVisible)
            {
                hand = "left";
                distance = Vector3.Distance(LeftHand.Index.Tip.Position, hit.transform.position);
            }

            // Only attempt to highlight a sphere if your finger is actually touching / close to touching.
            if (distance > Distance)
            {
                return;
            }

            // Only attempt to highlight when touching a new sphere (don't re-highlight each frame).
            if (PreviousRaycastHitTag == hit.transform.tag)
            {
                return;
            }

            PreviousRaycastHitTag = hit.transform.tag;
            Highlighted = GameObject.FindGameObjectWithTag(hit.transform.tag);
            SetColor(Highlighted, Color.cyan);

            StatusTextTimer = 0f;
            Status.text = $"Touching the {Highlighted.tag.ToLower()} sphere with your {hand} hand.";
            Status.text += "\n\r\n\rHold it between your index finger and thumb to grab it!";

            SetColor(Highlighted, Color.cyan);

            // Reset the color of any other spheres.
            foreach (GameObject sphere in Spheres)
            {
                if (sphere.tag != Highlighted.tag)
                {
                    ResetSphereColor(sphere);
                }
            }

            return;
        }

        // None of the spheres are being touched.
        if (PreviousRaycastHitTag != string.Empty)
        {
            SetColor(Sphere1, Color.red);
            SetColor(Sphere2, Color.blue);
            SetColor(Sphere3, Color.green);
            Highlighted = null;
            PreviousRaycastHitTag = string.Empty;

            if (StatusTextTimer == 0f)
            {
                Status.text = "Touch a sphere with your index finger.";
                Status.text += "\n\r\n\rHold your hand up to reset the spheres.";
            }

        }

    }

    /// <summary>
    /// Changes the color of the material applied to the specified sphere.
    /// </summary>
    /// <param name="sphere">The sphere who's color should be changed.</param>
    /// <param name="color">The color to change the material color to.</param>
    private void SetColor(GameObject sphere, Color color)
    {
        sphere.GetComponent<MeshRenderer>().material.color = color;
    }

    /// <summary>
    /// Cancels the velocity of the three spheres and resets them to their starting positions.
    /// </summary>
    private void ResetSpheres()
    {
        Sphere1.GetComponent<Rigidbody>().velocity = Vector3.zero;
        Sphere2.GetComponent<Rigidbody>().velocity = Vector3.zero;
        Sphere3.GetComponent<Rigidbody>().velocity = Vector3.zero;
        Sphere1.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        Sphere2.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        Sphere3.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        Sphere1.transform.position = new Vector3(-0.0625f, -0.1255f, 0.5f);
        Sphere2.transform.position = new Vector3(0.0f, 0.0f, 0.5f);
        Sphere3.transform.position = new Vector3(0.0625f, -0.1255f, 0.5f);
    }

    /// <summary>
    /// Resets the specified sphere back to its original color.
    /// </summary>
    private void ResetSphereColor(GameObject sphere)
    {
        switch (sphere.tag)
        {
            case "Red":
                {
                    SetColor(sphere, Color.red);
                    break;
                }

            case "Blue":
                {
                    SetColor(sphere, Color.blue);
                    break;
                }

            case "Green":
                {
                    SetColor(sphere, Color.green);
                    break;
                }
        }
    }

    /// <summary>
    /// Evaluates if a sphere is being held between the thumb and index finger.
    /// </summary>
    /// <param name="thumb">The vector3 for the keypoint at the tip of the thumb.</param>
    /// <param name="indexFinger">The vector3 for the keypoint at the tip of the index finger.</param>
    private bool Pinching(Vector3 thumb, Vector3 indexFinger)
    {
        Ray ray = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(indexFinger));

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // OverlapSphere is used for the thumb (rather than a Raycast from the camera) to help improve accuracy when one sphere is infront-of/behind another.
            Collider[] collidersThumb = Physics.OverlapSphere(thumb, 0.05f);

            foreach (Collider collider in collidersThumb)
            {
                // Thumb and index finger are touching the same sphere.
                if (collider.tag == hit.transform.tag)
                {
                    if (RightHand.IsVisible)
                    {
                        // Check to see if the right hand is in the C keypose (pinching gesture).
                        if (RightHand.KeyPose == MLHandKeyPose.C)
                        {
                            return true;
                        }

                        // If the right hand is pinching or in the OK keypose, make sure there is at least a small gap between the tips of the thumb and index finger.
                        if (RightHand.KeyPose == MLHandKeyPose.Pinch || RightHand.KeyPose == MLHandKeyPose.Ok)
                        {
                            if (Vector3.Distance(RightHand.Thumb.Tip.Position, RightHand.Index.Tip.Position) >= 0.0095f)
                            {
                                return true;
                            }
                        }

                        return false;
                    }

                    if (LeftHand.IsVisible)
                    {
                        // Check to see if the left hand is in the C keypose (pinching gesture).
                        if (LeftHand.KeyPose == MLHandKeyPose.C)
                        {
                            return true;
                        }

                        // If the left hand is pinching or in the OK keypose, make sure there is at least a small gap between the tips of the thumb and index finger.
                        if (LeftHand.KeyPose == MLHandKeyPose.Pinch || LeftHand.KeyPose == MLHandKeyPose.Ok)
                        {
                            if (Vector3.Distance(LeftHand.Thumb.Tip.Position, LeftHand.Index.Tip.Position) >= 0.0095f)
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Called via the invoke method after a delay to prevent accidental pickup.
    /// </summary>
    private void DelayedPickup()
    {     
        if (RightHand.IsVisible)
        {
            // When the right hand is still pinching the sphere, cancel velocity, reset it to its original color, and switch to the moving phase.
            if (Pinching(RightHand.Thumb.Tip.Position, RightHand.Index.Tip.Position))
            {
                HandInUse = MLHandType.Right;
                Pinched = GameObject.FindGameObjectWithTag(PreviousRaycastHitTag);
                Pinched.GetComponent<Rigidbody>().velocity = Vector3.zero;
                Pinched.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                ResetSphereColor(Pinched);
                Status.text = $"Holding the {Pinched.tag.ToLower()} sphere with your right hand.";
                Phase = Phases.Moving;
            }

            return;

        }

        if (LeftHand.IsVisible)
        {
            // When the left hand is still pinching the sphere, cancel velocity, reset it to its original color, and switch to the moving phase.
            if (Pinching(LeftHand.Thumb.Tip.Position, LeftHand.Index.Tip.Position))
            {
                HandInUse = MLHandType.Left;
                Pinched = GameObject.FindGameObjectWithTag(PreviousRaycastHitTag);
                ResetSphereColor(Pinched);
                Status.text = $"Holding the {Pinched.tag.ToLower()} sphere with your left hand.";
                Phase = Phases.Moving;
            }

            return;

        }
    }

    /// <summary>
    /// Drops the currently held sphere if the gap between the index finger and thumb is too wide, if the hand in use is no longer in view, or if the free hand becomes visible.
    /// </summary>
    private bool DropSphere()
    {
        if (!RightHand.IsVisible && !LeftHand.IsVisible)
        {
            return true;
        }

        switch (HandInUse)
        {
            case MLHandType.Right:
                {
                    if (!RightHand.IsVisible)
                    {
                        return true;
                    }

                    if (LeftHand.IsVisible)
                    {
                        return true;
                    }

                    break;
                }

            case MLHandType.Left:
                {
                    if (!LeftHand.IsVisible)
                    {
                        return true;
                    }

                    if (RightHand.IsVisible)
                    {
                        return true;
                    }

                    break;
                }
        }

        // Drop the sphere when the gap between the tips of the index finger and thumb is too wide.
        if (PinchLine.bounds.size.x > MaxPinchWidth)
        {
            return true;
        }

        return false;

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

}
