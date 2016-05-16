using UnityEngine;
using UnityEngine.UI;
using CnControls;
using System.Collections;

public class PlayerMoveController : MonoBehaviour {
    public float Speed;

    public Image Rec;

    private float h;
    private Vector3 v;
    private InputVCR vcr;
    
    // Use this for initialization
    void Awake() {
        vcr = GetComponent<InputVCR>();
    }

    void Start() {

    }

    void Update() {
        if(Input.GetKeyDown(KeyCode.Space)) {
            vcr.NewRecording();
            Rec.color = Color.red;
        }

        if(Input.GetKeyUp(KeyCode.Space)) {
            vcr.Play();
            Rec.color = Color.white;
        }

        if(Input.GetKeyDown(KeyCode.R)) {
            vcr.Play();
        }
    }

    // Update is called once per frame
    void LateUpdate() {
        if(vcr.mode == InputVCRMode.Playback) {
            h = vcr.GetAxis("Horizontal");
            transform.Rotate(Vector3.up, h);
            transform.Translate(transform.forward * Speed * Time.deltaTime, Space.Self);
        }
    }

    public void PlayBackEnd() {
        Rec.color = Color.green;
    }
}