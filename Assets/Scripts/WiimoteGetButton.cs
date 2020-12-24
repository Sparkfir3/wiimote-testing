using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WiimoteApi;

/* 
 * HOW TO SET UP
 * 
 * To set up this script, go to Edit > Project Settings > Script Execution Order
 * Set this script up to run before Default Time, and set the InputManager script to run before this one
 * (Same instructions are included in the InputManager script)
 */

public class WiimoteGetButton : MonoBehaviour {

    private Wiimote wiimote;

    private Button[] buttonTypes;
    public Dictionary<Button, bool> buttonDown = new Dictionary<Button, bool>();
    private Dictionary<Button, bool> buttonDownFlag = new Dictionary<Button, bool>();
    public Dictionary<Button, bool> buttonUp = new Dictionary<Button, bool>();
    private Dictionary<Button, bool> buttonUpFlag = new Dictionary<Button, bool>();

    private void Awake() {
        buttonTypes = (Button[])System.Enum.GetValues(typeof(Button));
        foreach(Button type in buttonTypes) {
            buttonDown.Add(type, false);
            buttonDownFlag.Add(type, false);
            buttonUp.Add(type, false);
            buttonUpFlag.Add(type, false);
        }
    }

    private void Update() {
        wiimote = InputManager.wiimote;
        if(wiimote == null) {
            foreach(Button type in buttonTypes) {
                buttonDown[type] = false;
                buttonDownFlag[type] = false;
                buttonUp[type] = false;
                buttonUpFlag[type] = false;
            }
            return;
        }

        foreach(Button type in buttonTypes) {
            // Button pressed
            if(GetCorrespondingWiimoteButton(type)) {
                // Down - check
                if(!buttonDownFlag[type]) {
                    buttonDown[type] = true;
                    buttonDownFlag[type] = true;
                } else
                    buttonDown[type] = false;

                // Up - set false
                buttonUp[type] = false;
                buttonUpFlag[type] = false;

            // Button not pressed
            } else { 
                // Down - set false
                buttonDown[type] = false;
                buttonDownFlag[type] = false;

                // Up - check
                if(!buttonUpFlag[type]) {
                    buttonUp[type] = true;
                    buttonUpFlag[type] = true;
                } else
                    buttonUp[type] = false;
            }
        }
    }

    public bool GetCorrespondingWiimoteButton(Button button) {
        switch(button) {
            case Button.A:
                return wiimote.Button.a;
            case Button.B:
                return wiimote.Button.b;
            case Button.Up:
                return wiimote.Button.d_up;
            case Button.Down:
                return wiimote.Button.d_down;
            case Button.Left:
                return wiimote.Button.d_left;
            case Button.Right:
                return wiimote.Button.d_right;
            case Button.Plus:
                return wiimote.Button.plus;
            case Button.Minus:
                return wiimote.Button.minus;
            case Button.Home:
                return wiimote.Button.home;
            case Button.One:
                return wiimote.Button.one;
            case Button.Two:
                return wiimote.Button.two;
            case Button.Z:
            case Button.C:
                return GetNunchuckButton(button);
            default:
                return false;
        }
    }

    private bool GetNunchuckButton(Button button) {
        if(wiimote.current_ext != ExtensionController.NUNCHUCK) {
            //Debug.LogError("Nunchuck not detected");
            return false;
        }

        NunchuckData data = wiimote.Nunchuck;
        if(button == Button.Z)
            return data.z;
        else if(button == Button.C)
            return data.c;
        else
            return false;
    }

}