using UnityEngine;
using System.Collections;

public class Record : MonoBehaviour {
    public bool recording;

	// Use this for initialization
	void Start () {
        recording = false;
	}
	
	void FixedUpdate () {
        if(Input.GetKey(KeyCode.Space)) {
            if(!recording) {
                recording = true;
            } else {
                Debug.Log(Input.GetKey(KeyCode.LeftArrow));
                Debug.Log(Input.GetKey(KeyCode.RightArrow));
            }
        } else {
            if(recording) {
                recording = false;
                Debug.ClearDeveloperConsole();
            }
        }
	}
}
