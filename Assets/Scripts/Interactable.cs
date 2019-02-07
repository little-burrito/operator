using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum InteractableSystemType { DOOR_OPENING_MECHANISM, DOOR_PROXIMITY_SENSOR, SAFE_LEAK_CODES, CUSTOM }

public class Interactable : MonoBehaviour {

    public GameObject textPrefab;
    public int id = 0;
    public int nameLength = 5;
    public string displayName = "";
    public GameObject nameFront;
    public float maxRenderDistance = 10.0f;

    public List<InteractableSystem> systems;
    public InteractableSystemType[] systemId;
    public bool[] systemEnabled;
    public bool[] systemLocked;

    private Animator anim;

	// Use this for initialization
	public void Start () {
        Init();
	}

    public void Init() {
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
	public void Update () {
        RotateTextTowardsCamera();
        HideOrShowText();
        if ( anim != null ) {
            foreach ( InteractableSystem isystem in systems ) {
                anim.SetBool( isystem.name, isystem.enabled );
            }
        }
	}

    public void RotateTextTowardsCamera() {
        nameFront.transform.position = transform.position;
        Vector3 positionDifference = Camera.main.transform.position - transform.position;
        nameFront.transform.rotation = Quaternion.LookRotation( positionDifference ) * Quaternion.Euler( 0, 180, 0 );
    }
    public void HideOrShowText() {
        Vector3 positionDifference = Camera.main.transform.position - transform.position;
        if ( positionDifference.magnitude > maxRenderDistance ) {
            nameFront.active = false;
        } else {
            bool objectViewObstructed = Physics.Raycast( gameObject.transform.position, positionDifference, ( float )positionDifference.magnitude, LayerMask.NameToLayer( "Interaction ray passthrough" ) );
            if ( !objectViewObstructed ) {
                nameFront.active = true;
                Debug.DrawRay( gameObject.transform.position, positionDifference, Color.green );
            } else {
                nameFront.active = false;
                Debug.DrawRay( gameObject.transform.position, positionDifference, Color.red );
            }
        }
    }

    // Add system
    public InteractableSystem generateSystem( bool enabled, bool locked ) {
        InteractableSystem sys = new InteractableSystem( InteractableSystemType.CUSTOM, enabled, locked, systems );
        systems.Add( sys );
        return sys;
    }
}

public class InteractableSystem {

    public string id;
    public string name;
    public InteractableSystemType systemType;
    public bool enabled;
    public bool locked;
    public List<InteractableSystem> parent;
    public int nameLength = 5;
    public string response = "";
    public delegate void Enable();
    public Enable enableSystem;
    public delegate void Disable();
    public Disable disableSystem;
    public delegate bool TestLock();
    public TestLock testLock;
    public int identifier;

    public InteractableSystem( InteractableSystemType systemType, bool enabled, bool locked, List<InteractableSystem> parent ) {
        this.identifier = 0;
        this.systemType = systemType;
        this.enabled = enabled;
        this.locked = locked;
        this.parent = parent;
        testLock = () => { return this.locked; };
        enableSystem = () => { this.enabled = true; };
        disableSystem = () => { this.enabled = false; };
        switch ( systemType ) {
            case InteractableSystemType.DOOR_PROXIMITY_SENSOR:
                name = "Proximity sensor";
                break;
            case InteractableSystemType.DOOR_OPENING_MECHANISM:
                name = "Opening mechanism";
                testLock = () => {
                    foreach ( InteractableSystem isystem in parent ) {
                        if ( isystem.systemType == InteractableSystemType.DOOR_PROXIMITY_SENSOR ) {
                            if ( isystem.enabled ) {
                                return true;
                            } else {
                                return false;
                            }
                        }
                    }
                    return false;
                };
                break;
            case InteractableSystemType.SAFE_LEAK_CODES:
                name = "Code leaker...";
                break;
            default:
                name = "CUSTOM SYSTEM";
                break;
        }
        string[] characters = new string[] { "R", "B", "A", "N", "J", "Y", "V", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        for ( int i = 0; i < nameLength; i++ ) {
            id += characters[ Random.Range( 0, characters.Length ) ];
        }
    }

    public void activate() {
        this.locked = testLock();
        if ( !locked ) {
            enableSystem();
            enabled = true;
            response = "System enabled";
        } else {
            response = "Unable to enable - system is locked";
        }
    }

    public void deactivate() {
        this.locked = testLock();
        if ( !locked ) {
            disableSystem();
            enabled = false;
            response = "System disabled";
        } else {
            response = "Unable to disable - system is locked";
        }
    }
}
