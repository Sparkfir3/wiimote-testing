using UnityEngine;
using System.Collections;
using WiimoteApi;

public class Cube : MonoBehaviour {

    public enum TestMode { Button, Acceleration, WMP, Nunchuck, Pointer }

    public TestMode mode;
    private Rigidbody rb;
    
    [Tooltip("Speed multiplier for when object is being dragged by the pointer.")]
    public float dragSpeedMultiplier;
    private bool held;

    [Tooltip("Move speed while using the nunchuck.")]
    public float nunchuckSpeed;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
    }

	private void Update() {
        if(InputManager.wiimote != null) {
            // Set variables
            if(mode != TestMode.Pointer)
                held = false;

            // Switch modes
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

                case TestMode.Nunchuck:
                    try {
                        Vector3 axis = new Vector3(InputManager.inputs.GetNunchuckAxis("Horizontal"), 0f, InputManager.inputs.GetNunchuckAxis("Vertical"));
                        Debug.Log(axis);
                        rb.velocity = axis * nunchuckSpeed;
                    } catch(System.Exception e) {
                        Debug.LogError(e + InputManager.wiimote.current_ext.ToString());
                    }
                    break;

                // ------------------------------------------------

                case TestMode.Pointer:
                    // Pick up
                    if(!held && InputManager.wiimote.Button.b && InputManager.inputs.AimingAtObject(gameObject))
                        held = true;
                    else if(!InputManager.wiimote.Button.b || InputManager.inputs.pointer.anchorMin[0] == -1f) // Button released or pointer offscreen
                        held = false;
                    if(!held)
                        break;

                    // Move
                    float offsetFromCamera = 10;
                    Vector3 direction = InputManager.inputs.PointerToWorldPos(offsetFromCamera) - transform.position;
                    rb.velocity = direction * dragSpeedMultiplier;

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
