using UnityEngine;
using System.Collections;
using WiimoteApi;

public class Cube : MonoBehaviour {

    public enum TestMode { Button, Acceleration, WMP }

    public TestMode mode;
    private Rigidbody rb;

    MotionPlusData wmpData;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
    }

	private void Update() {
        if(InputManager.wiimote != null) {
            switch(mode) {
                case TestMode.Button:
                    if(InputManager.wiimote.Button.b) 
                        rb.velocity = Vector3.up * 5f;
                    break;

                case TestMode.Acceleration:
                    rb.velocity = InputManager.GetAccelVector();
                    break;

                case TestMode.WMP:
                    if(InputManager.wiimote.wmp_attached) {
                        wmpData = InputManager.wiimote.MotionPlus;
                        rb.velocity = new Vector3(0, wmpData.YawSpeed, 0);
                        Debug.Log(rb.velocity);
                    }
                    break;

                default:
                    break;
            }
        }

        if(Input.GetKeyDown(KeyCode.Space)) {
            transform.position = new Vector3(0, 1, 0);
        } else if(Input.GetKeyDown(KeyCode.UpArrow)) {
            if(mode == TestMode.WMP)
                mode = 0;
            else
                mode++;
        }
    }

}
