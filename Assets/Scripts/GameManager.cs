using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {

	public static GameManager gm;

    private InputManager inputs;

    private void Awake() {
        // Singleton initialization
        if(gm == null) {
            gm = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }

        // Other
        inputs = GetComponent<InputManager>();
        InputManager.inputs = inputs;
    }

}
