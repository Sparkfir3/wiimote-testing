using UnityEngine;
using System.Collections;
using WiimoteApi;

public class InputManager : MonoBehaviour {

    public static InputManager inputs;
    public static Wiimote wiimote;

    private Vector3 wmpOffset;

    public RectTransform pointer;

    private void Update() {
        // Find wiimote
        if(!WiimoteManager.HasWiimote()) {
            Debug.Log("Finding wiimote...");
            if(!FindWiimote())
                return; // Exit if no wiimote found
        }
        wiimote = WiimoteManager.Wiimotes[0];

        int ret;
        do {
            // IMPORTANT - this variable assignment step stops controller latency?
            // yeah I'm confused too but oh well
            ret = wiimote.ReadWiimoteData();

            // WMP stuff
            if(ret > 0 && wiimote.current_ext == ExtensionController.MOTIONPLUS) {
                Vector3 offset = new Vector3(-wiimote.MotionPlus.PitchSpeed,
                                                wiimote.MotionPlus.YawSpeed,
                                                wiimote.MotionPlus.RollSpeed) / 95f; // Divide by 95Hz (average updates per second from wiimote)
                wmpOffset += offset;
            }
        } while(ret > 0);
        
        if(wiimote.Button.a) {
            Debug.Log(GetAccelVector());
        }

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

        // Pointer
        float[] pointerPos = wiimote.Ir.GetPointingPosition();
        pointer.anchorMin = new Vector2(pointerPos[0], pointerPos[1]);
        pointer.anchorMax = new Vector2(pointerPos[0], pointerPos[1]);
    }

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
            wiimote.SetupIRCamera();

            return true;
        } else {
            wiimote = null;
            return false;
        }
    }

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

    public Vector3 PointerToWorldPos(float forwardOffset) {
        if(pointer.anchorMin == new Vector2(-1f, -1f))
            return Vector3.zero;
        
        return Camera.main.ViewportToWorldPoint(new Vector3(pointer.anchorMin.x, pointer.anchorMin.y, forwardOffset));
    }

    #endregion Pointer

    // ---------------------------------------------------------------------------------------------

    public Vector3 GetAccelVector() {
        float accel_x;
        float accel_y;
        float accel_z;

        float[] accel = wiimote.Accel.GetCalibratedAccelData();
        accel_x = accel[0];
        accel_y = -accel[2];
        accel_z = -accel[1];

        return new Vector3(accel_x, accel_y, accel_z).normalized;
    }
	
    private void OnApplicationQuit() {
        if(wiimote != null) {
            WiimoteManager.Cleanup(wiimote);
            wiimote = null;
        }
    }

}
