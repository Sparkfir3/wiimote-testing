using UnityEngine;
using System.Collections;
using WiimoteApi;

public class Cube : MonoBehaviour {

    public enum TestMode { Button, Acceleration, WMP, Nunchuck, Shake, Twist, Pointer }

    public TestMode mode;
    private Rigidbody rb;

    [Header("Nunchuck")]
    [Tooltip("Move speed while using the nunchuck.")]
    public float nunchuckSpeed;

    [Header("Shake/Twist")]
    public float shakeCooldown;
    public float shakeJump, shakeSpin;
    private float shakeTimer; // Timer for calculating shake cooldown. 0 is resting, and counts down from shakeCooldown

    [Header("Pointer")]
    [Tooltip("Speed multiplier for when object is being dragged by the pointer.")]
    public float dragSpeedMultiplier;
    private bool held;

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

                case TestMode.Shake:
                    if(shakeTimer == 0) {
                        if(InputManager.inputs.Shake) {
                            rb.velocity = new Vector3(0f, shakeJump, 0f);
                            rb.angularVelocity = new Vector3(0f, 1f, 1f).normalized * shakeSpin;
                            shakeTimer = shakeCooldown;
                        }
                    } else {
                        shakeTimer = Mathf.Clamp(shakeTimer - Time.deltaTime, 0, shakeCooldown);
                    }
                    break;

                // ------------------------------------------------

                case TestMode.Twist:
                    if(shakeTimer == 0) {
                        if(InputManager.inputs.Twisting) {
                            rb.velocity = new Vector3(0f, shakeJump, 0f);
                            rb.angularVelocity = new Vector3(0f, 1f, 1f).normalized * shakeSpin;
                            shakeTimer = shakeCooldown;
                        }
                    } else {
                        shakeTimer = Mathf.Clamp(shakeTimer - Time.deltaTime, 0, shakeCooldown);
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

        // --------

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
