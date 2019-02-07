using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WinchController : Interactable {

    public SpringJoint winchJoint;
    public GameObject lights;

	// Use this for initialization
    void Start() {
        Init();

        // Winch lock
        InteractableSystem system = generateSystem( true, false );
        system.name = "Winch lock";
        system.identifier = 1;
        system.disableSystem = () => {
            Destroy( winchJoint );
            system.locked = true;
        };
        system.enableSystem = () => {
            //winchJoint.active = true;
        };

        // Light control
        InteractableSystem system2 = generateSystem( false, false );
        system2.name = "Light control";
        system2.identifier = 2;
        system2.disableSystem = () => {
            lights.active = false;
        };
        system2.enableSystem = () => {
            lights.active = true;
        };
    }
}
