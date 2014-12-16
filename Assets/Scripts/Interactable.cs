using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum InteractableSystemType { DOOR_OPENING_MECHANISM, DOOR_PROXIMITY_SENSOR, SAFE_LEAK_CODES }

public class Interactable : MonoBehaviour {

    public GameObject textPrefab;
    public int id = 0;
    public int nameLength = 5;
    public string displayName = "";
    private GameObject nameFront;
    public float maxRenderDistance = 10.0f;

    public List<InteractableSystem> systems;
    public InteractableSystemType[] systemId;
    public bool[] systemEnabled;
    public bool[] systemLocked;

    private Animator anim;

	// Use this for initialization
	void Start () {
        //Random.seed = ( int )System.DateTime.Now.Ticks + this.GetInstanceID();
        if ( displayName == "" ) {
            string[] characters = new string[] { "X", "D", "Z", "L", "H", "S", "M", "P", "T", "Q", "W", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            for ( int i = 0; i < nameLength; i++ ) {
                displayName += characters[ Random.Range( 0, characters.Length ) ];
            }
        }
        nameFront = Instantiate( textPrefab ) as GameObject;
        TextMesh textMesh = nameFront.GetComponent<TextMesh>();
        textMesh.text = gameObject.name + "\nId: " + displayName;
        RotateTextTowardsCamera();
        HideOrShowText();
        systems = new List<InteractableSystem>();
        anim = gameObject.GetComponent<Animator>();

        for ( int i = 0; i < systemId.Length; i++ ) {
            InteractableSystemType ist = systemId[ i ];
            bool sysEnabled = systemEnabled[ i ];
            bool sysLocked = systemLocked[ i ];
            InteractableSystem sys = new InteractableSystem( ist, sysEnabled, sysLocked, systems );
            systems.Add( sys );
        }
	}
	
	// Update is called once per frame
	void Update () {
        RotateTextTowardsCamera();
        HideOrShowText();
        foreach ( InteractableSystem isystem in systems ) {
            anim.SetBool( isystem.displayName, isystem.enabled );
        }
	}

    void RotateTextTowardsCamera() {
        nameFront.transform.position = transform.position;
        Vector3 positionDifference = Camera.main.transform.position - transform.position;
        nameFront.transform.rotation = Quaternion.LookRotation( positionDifference ) * Quaternion.Euler( 0, 180, 0 );
    }
    void HideOrShowText() {
        Vector3 positionDifference = Camera.main.transform.position - transform.position;
        if ( positionDifference.magnitude > maxRenderDistance ) {
            nameFront.active = false;
        } else {
            nameFront.active = true;
        }
    }
}

public class InteractableSystem {

    public string name;
    public string displayName;
    public InteractableSystemType id;
    public bool enabled;
    public bool locked;
    private List<InteractableSystem> parent;
    public int nameLength = 5;
    public string response = "";

    public InteractableSystem( InteractableSystemType id, bool enabled, bool locked, List<InteractableSystem> parent ) {
        this.id = id;
        this.enabled = enabled;
        this.locked = locked;
        this.parent = parent;
        switch ( id ) {
            case InteractableSystemType.DOOR_PROXIMITY_SENSOR:
                displayName = "Proximity sensor";
                break;
            case InteractableSystemType.DOOR_OPENING_MECHANISM:
                displayName = "Opening mechanism";
                break;
            case InteractableSystemType.SAFE_LEAK_CODES:
                displayName = "Code leaker...";
                break;
        }
        string[] characters = new string[] { "R", "B", "A", "N", "J", "Y", "V", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        for ( int i = 0; i < nameLength; i++ ) {
            name += characters[ Random.Range( 0, characters.Length ) ];
        }
    }

    public void activate() {
        testLock();
        if ( !locked ) {
            enabled = true;
            response = "System enabled";
        } else {
            response = "Unable to enable - system is locked";
        }
    }

    public void deactivate() {
        testLock();
        if ( !locked ) {
            enabled = false;
            response = "System disabled";
        } else {
            response = "Unable to disable - system is locked";
        }
    }

    public void testLock() {
        switch ( id ) {
            case InteractableSystemType.DOOR_OPENING_MECHANISM: {
                foreach ( InteractableSystem isystem in parent ) {
                    if ( isystem.id == InteractableSystemType.DOOR_PROXIMITY_SENSOR ) {
                        if ( isystem.enabled ) {
                            locked = true;
                            break;
                        } else {
                            locked = false;
                            break;
                        }
                    }
                }
                break;
            }
        }
    }
}