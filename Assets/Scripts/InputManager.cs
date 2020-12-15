using UnityEngine;
using System.Collections;
using WiimoteApi;

public class InputManager : MonoBehaviour {
    
    public static Wiimote wiimote;

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
                Debug.Log("Wii Motion Plus reset");
            }
        }
        
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

            return true;
        } else {
            wiimote = null;
            return false;
        }
    }

    private bool FindWMP() {
        if(wiimote.wmp_attached) {
            wiimote.ActivateWiiMotionPlus();
            return true;
        }
        return false;
    }

    public static Vector3 GetAccelVector() {
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
