using UnityEngine;
using System.Collections;
using WiimoteApi;

/* 
 * HOW TO SET UP
 * 
 * To set up this script, go to Edit > Project Settings > Script Execution Order
 * Set the WiimoteGetButton script to run before Default Time, and set this script to run before that one
 * (Same instructions are included in the WiimoteGetButton script)
 */

public enum Button { A, B, Up, Down, Left, Right, Plus, Minus, Home, One, Two, Z, C };

public class InputManager : MonoBehaviour {

    public static InputManager inputs;
    public static Wiimote wiimote;
    private WiimoteGetButton getButton;

    private Vector3 wmpOffset;

    [Header("Pointer")]
    public RectTransform pointer;
    public RectTransform[] irAnchors;
    public bool pointerSmoothing;
    public bool pointerRotate;
    private Camera cam;

    [Header("Shake/Twist Detection")]
    private bool _shaking;
    private Vector3 prevAccelValue;
    private float[] prevAccelAngles = new float[5];
    private float _twistAmount, prevTwistValue;

    // ---------------------------------------------------------------------------------------------

    private void Awake() {
        getButton = GetComponent<WiimoteGetButton>();
    }

    private void Start() {
        cam = Camera.main;
    }

    // ---------------------------------------------------------------------------------------------

    private void Update() {
        // Find wiimote
        if(!WiimoteManager.HasWiimote()) {
            Debug.Log("Finding wiimote...");
            if(!FindWiimote())
                return; // Exit if no wiimote found
        }
        wiimote = WiimoteManager.Wiimotes[0];

        if(GetWiimoteButtonDown(Button.One))
            Debug.Log("test");

        // ---

        int ret;
        do {
            // IMPORTANT - this variable assignment step stops controller latency?
            // Specifically the assignment part, not the read function.
            // Yeah I'm confused too but oh well
            ret = wiimote.ReadWiimoteData();

            // WMP stuff
            if(ret > 0 && wiimote.current_ext == ExtensionController.MOTIONPLUS) {
                Vector3 offset = new Vector3(-wiimote.MotionPlus.PitchSpeed,
                                                wiimote.MotionPlus.YawSpeed,
                                                wiimote.MotionPlus.RollSpeed) / 95f; // Divide by 95Hz (average updates per second from wiimote)
                wmpOffset += offset;
            }
        } while(ret > 0);

        #region Button Debugs and Toggles

        /*if(wiimote.Button.a) {
            Debug.Log(GetAccelVector());
        }*/

        // Wii motion plus
        if(Input.GetKeyDown(KeyCode.Q)) {
            wiimote.RequestIdentifyWiiMotionPlus();
            Debug.Log("Wii motion plus setting up...");
        }
        if(Input.GetKeyDown(KeyCode.W)) {
            wiimote.RequestIdentifyWiiMotionPlus();
            if(FindWMP()) {
                Debug.Log("Wii Motion Plus enabled");
            } else {
                Debug.Log("Wii Motion Plus failed to enable");
            }
        }

        // Accelerometer
        if(Input.GetKeyDown(KeyCode.R)) {
            wiimote.Accel.CalibrateAccel(AccelCalibrationStep.A_BUTTON_UP);
            Debug.Log("Accelerometer recalibrated");

            if(wiimote.current_ext == ExtensionController.MOTIONPLUS) {
                wiimote.MotionPlus.SetZeroValues();
                wmpOffset = Vector3.zero;
                Debug.Log("Wii Motion Plus reset");
            }
        }

        // Input mode
        if(Input.GetKeyDown(KeyCode.I)) {
            wiimote.SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL_EXT16);
            Debug.Log("Updated wiimote input data type");
        }

        #endregion

        // ---

        #region Pointer

        // Pointer anchors and rotation
        if(pointerRotate) {
            if(irAnchors.Length < 2) {
                Debug.LogError("IR anchors not found");
                return;
            }

            foreach(RectTransform anchor in irAnchors)
                anchor.gameObject.SetActive(true);
            
            float[,] ir = wiimote.Ir.GetProbableSensorBarIR();
            /*string output = "";
            for(int i = 0; i < ir.Length; i++) {
                output += "{" + ir + "}\n";
            }
            Debug.Log(output);*/

            for(int i = 0; i < 2; i++) {
                float x = ir[i, 0] / 1023f;
                float y = ir[i, 1] / 767f;
                if(x == -1 || y == -1) {
                    irAnchors[i].anchorMin = new Vector2(0, 0);
                    irAnchors[i].anchorMax = new Vector2(0, 0);
                }

                irAnchors[i].anchorMin = new Vector2(x, y);
                irAnchors[i].anchorMax = new Vector2(x, y);
            }
            pointer.rotation = GetPointerRotation(irAnchors[0].localPosition, irAnchors[1].localPosition);
        } else {
            foreach(RectTransform anchor in irAnchors)
                anchor.gameObject.SetActive(false);
            pointer.transform.rotation = Quaternion.identity;
        }

        // Pointer
        Vector2 pointerPos;
        if(pointerSmoothing)
            pointerPos = StabilizePointerPos(pointer.anchorMax, new Vector2(wiimote.Ir.GetPointingPosition()[0], wiimote.Ir.GetPointingPosition()[1])); // Smoothed
        else
            pointerPos = new Vector2(wiimote.Ir.GetPointingPosition()[0], wiimote.Ir.GetPointingPosition()[1]); // Unsmoothed
        pointer.anchorMin = new Vector2(pointerPos[0], pointerPos[1]);
        pointer.anchorMax = new Vector2(pointerPos[0], pointerPos[1]);

        #endregion

        // ---

        // Shake and twist
        if(Time.frameCount % 2 == 0) { // Every 2 frames
            CalculateShake();
            CalculateTwist();
        }
    }

    // -----------------------------------

    private bool FindWiimote() {
        WiimoteManager.FindWiimotes();

        if(WiimoteManager.HasWiimote()) {
            wiimote = WiimoteManager.Wiimotes[0];
            wiimote.SendPlayerLED(true, false, false, false);

            // Mode = acceleration + extensions
            wiimote.SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL_EXT16);

            // Acceleration
            //wiimote.Accel.CalibrateAccel(AccelCalibrationStep.A_BUTTON_UP);

            // IR
            wiimote.SetupIRCamera(IRDataType.EXTENDED);

            Debug.Log("Wiimote found and set up");
            return true;
        } else {
            wiimote = null;
            return false;
        }
    }

    // ---------------------------------------------------------------------------------------------

    #region Get Wiimote Buttons
    /// Equivalent of Input.GetButton and its variants, but for the wii remote

    public bool GetWiimoteButton(Button button) {
        return getButton.GetCorrespondingWiimoteButton(button);
    }

    public bool GetWiimoteButtonDown(Button button) {
        return getButton.buttonDown[button];
    }

    public bool GetWiimoteButtonUp(Button button) {
        return getButton.buttonUp[button];
    }

    #endregion

    // ---------------------------------------------------------------------------------------------

    #region Wii Motion Plus

    private bool FindWMP() {
        if(wiimote.wmp_attached) {
            wiimote.ActivateWiiMotionPlus();
            return true;
        }
        return false;
    }

    public Vector3 WMPVectorStandardized() {
        if(!wiimote.wmp_attached)
            return Vector3.zero;

        Vector3 wmp = Vector3.zero;
        MotionPlusData data = wiimote.MotionPlus;

        //Debug.Log(data.YawSpeed);
        if(Mathf.Abs(data.YawSpeed) > 60)
            wmp.y = data.YawSpeed / 10;

        return wmp;
    }

    #endregion

    // ---------------------------------------------------------------------------------------------

    #region Pointer

    /// <summary>
    /// Returns the pointer's rotation in relation to its two IR anchors.
    /// </summary>
    /// <param name="anchorAPos">Position of the first IR anchor</param>
    /// <param name="anchorBPos">Position of the second IR anchor</param>
    private Quaternion GetPointerRotation(Vector2 anchorAPos, Vector2 anchorBPos) {
        bool faceUp = false;
        if(wiimote.Accel.GetCalibratedAccelData()[2] > 0)
            faceUp = true;

        Vector2 reference = (anchorBPos - anchorAPos).normalized;
        /*if(AnchorsFlipped(faceUp)) {
            reference *= -1f;
        }*/

        float angle = Vector2.Angle(Vector2.right, reference);
        if(reference.y > 0)
            angle = 360f - angle;

        // Bound angle to 0-360
        angle = angle % 360;
        if(angle < 0)
            angle += 360;

        //Debug.Log(reference + " // " + angle + " // Face up: " + faceUp);
        return Quaternion.Euler(0, 0, angle);
    }
    
    /*private bool AnchorsFlipped(bool faceUp) {
        return faceUp && pointer.transform.rotation.eulerAngles.z > 90 && pointer.transform.rotation.eulerAngles.z < 270;
    }*/

    /// <summary>
    /// Returns a lerped/stabilized position for the pointer
    /// </summary>
    /// <param name="basePos">The pointer's current position</param>
    /// <param name="newPos">The position the pointer is attempting to go towards</param>
    private Vector3 StabilizePointerPos(Vector3 basePos, Vector3 newPos) {
        float distance = (newPos - basePos).magnitude;

        if(distance < 0.03f)
            return Vector2.Lerp(basePos, newPos, 0.3f);
        else if(distance < 0.05f)
            return Vector2.Lerp(basePos, newPos, 0.7f);
        return newPos;
    }

    /// <summary>
    /// Returns the world position corresponding to the pointer with the given offset
    /// </summary>
    /// <param name="forwardOffset">Units forward from the camera the world position is</param>
    public Vector3 PointerToWorldPos(float forwardOffset) {
        if(pointer.anchorMin == new Vector2(-1f, -1f))
            return Vector3.zero;
        
        return cam.ViewportToWorldPoint(new Vector3(pointer.anchorMin.x, pointer.anchorMin.y, forwardOffset));
    }

    /// <summary>
    /// Detects whether or not the pointer is currently aiming at the given game object
    /// </summary>
    /// <param name="obj">Game object to look for</param>
    /// <param name="maxDistance">Maximum distance to check</param>
    public bool AimingAtObject(GameObject obj, float maxDistance = 15f) {
        Vector3 pointerPos = cam.ViewportToWorldPoint(new Vector3(pointer.anchorMin.x, pointer.anchorMin.y, Camera.main.nearClipPlane));
        Vector3 direction = (pointerPos - cam.transform.position).normalized;

        RaycastHit hit;
        if(Physics.Raycast(cam.transform.position, direction, out hit, maxDistance)) {
            if(hit.collider.gameObject == obj)
                return true;
        }
        return false;
    }

    #endregion Pointer

    // ---------------------------------------------------------------------------------------------

    #region Nunchuck

    /// <summary>
    /// Returns a value from -1.0f to 1.0f, representing the joystick's position in the given axis
    /// </summary>
    /// <param name="axis">The axis the check for, either "Horizontal" or "Vertical"</param>
    public float GetNunchuckAxis(string axis) {
        if(wiimote.current_ext != ExtensionController.NUNCHUCK)
            throw new System.Exception("Nunchuck not detected");
        
        NunchuckData data = wiimote.Nunchuck;
        int value = 0;
        switch(axis) {
            case "Horizontal":
                value = data.stick[0]; // General range is 35-228
                break;
            case "Vertical":
                value = data.stick[1]; // General range is 27-220
                break;
            default:
                throw new System.ArgumentException("Invalid argument: " + axis + ", expected \"Horizontal\" or \"Vertical\"");
        }

        // Check if input mode not setup
        if(value == 0) {
            wiimote.SendDataReportMode(InputDataType.REPORT_BUTTONS_ACCEL_EXT16);
            return 0f;
        }

        // Center is around 128
        if(value > 112 && value < 144)
            return 0f;
        
        // Set horizontal to similar range as vertical
        if(axis == "Horizontal")
            value -= 8;

        // Check for upper/lower bounds
        if(value > 200)
            return 1f;
        else if(value < 47)
            return -1f;

        // Return normalized value
        float normalizedValue = (value - 128f) / 128f;
        return Mathf.Clamp(normalizedValue, -1f, 1f);
    }

    #endregion

    // ---------------------------------------------------------------------------------------------

    #region Accelerometer

    /// <summary>
    /// Returns the wiimote's acceleration data as a vector, normalized.
    /// </summary>
    public Vector3 GetAccelVector() {
        return GetAccelVectorRaw().normalized;
    }

    /// <summary>
    /// Returns the wiimote's acceleration data as a vector.
    /// </summary>
    public Vector3 GetAccelVectorRaw() {
        float accel_x;
        float accel_y;
        float accel_z;

        float[] accel = wiimote.Accel.GetCalibratedAccelData();
        accel_x = accel[0];
        accel_y = accel[2];
        accel_z = accel[1];

        return new Vector3(accel_x, accel_y, accel_z);
    }

    // -----------------------------------

    private void CalculateShake() {
        // Calculate
        Vector3 nextAccel = GetAccelVector();
        float angle = Vector3.Angle(nextAccel, prevAccelValue);
        bool[] flags = new bool[prevAccelAngles.Length - 1];

        for(int i = 0; i < prevAccelAngles.Length - 1; i++) {
            if(prevAccelAngles[i] < 60) {
                for(int j = 0; j < flags.Length; j++) {
                    if(!flags[j]) {
                        flags[j] = true;
                        break;
                    }
                }
            }
        }

        // Determine shaking
        _shaking = !flags[flags.Length - 1];

        // Update accel and angles
        for(int i = 0; i < prevAccelAngles.Length - 1; i++) {
            prevAccelAngles[i] = prevAccelAngles[i + 1];
        }
        prevAccelValue = nextAccel;
        prevAccelAngles[prevAccelAngles.Length - 1] = angle;
    }

    /// <summary>
    /// Whether or not the wiimote is shaking.
    /// </summary>
    public bool Shake {
        get {
            return _shaking;
        }
    }
    
    // -----------------------------------

    private void CalculateTwist() {
        // Calculate
        float accel = GetAccelVector().x;
        _twistAmount = accel - prevTwistValue;
        //Debug.Log("A = " + accel + ", B = " + prevTwistValue + ", dif = " + _twistAmount);
        prevTwistValue = accel;
    }

    public bool Twisting {
        get {
            return Mathf.Abs(_twistAmount) > 0.6f;
        }
    }

    public float TwistAmount {
        get {
            return _twistAmount;
        }
    }

    #endregion

    // ---------------------------------------------------------------------------------------------

    private void OnApplicationQuit() {
        if(wiimote != null) {
            WiimoteManager.Cleanup(wiimote);
            wiimote = null;
        }
    }

}
