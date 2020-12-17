using UnityEngine;
using System.Collections;
using WiimoteApi;

public class Cube : MonoBehaviour {

    public enum TestMode { Button, Acceleration, WMP, Pointer }

    public TestMode mode;
    private Rigidbody rb;

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

                // ------------------------------------------------

                case TestMode.Acceleration:
                    rb.velocity = InputManager.inputs.GetAccelVector();
                    break;

                // ------------------------------------------------

                case TestMode.WMP:
                    Vector3 newVelocity = InputManager.inputs.WMPVectorStandardized();
                    /*if(newVelocity.magnitude > 0)
                        Debug.Log(newVelocity);*/
                    
                    if(newVelocity.y == 0)
                        newVelocity.y = rb.velocity.y;

                    if(newVelocity.magnitude > 1f)
                        rb.velocity = newVelocity;
                    break;

                // ------------------------------------------------

                case TestMode.Pointer:
                    if(InputManager.wiimote.Button.b) {
                        Vector3 newPos = InputManager.inputs.PointerToWorldPos(10f);
                        if(newPos != Vector3.zero) {
                            transform.position = newPos;
                        }
                    }

                    break;

                // ------------------------------------------------

                default:
                    break;
            }
        }

        if(Input.GetKeyDown(KeyCode.Space)) {
            transform.position = new Vector3(0, 1, 0);
            rb.velocity = Vector3.zero;
        } else if(Input.GetKeyDown(KeyCode.UpArrow)) {
            if(mode == TestMode.Pointer)
                mode = 0;
            else
                mode++;
            
            Debug.Log("Cube test mode set to " + mode.ToString());
        } else if(Input.GetKeyDown(KeyCode.DownArrow)) {
            if(mode == TestMode.Button)
                mode = TestMode.Pointer;
            else
                mode--;

            Debug.Log("Cube test mode set to " + mode.ToString());
        }
    }

}
