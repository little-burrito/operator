using UnityEngine;
using System.Collections;

public class Level1 : Agent {

    private int waypoint = 0;
    private Animator anim;

	// Use this for initialization
    void Start() {
        initializeAgent();
        init = () => {
            Invoke( "testWaypoint", 1.0f );
            StartCoroutine( firstDialog() );
        };
        anim = gameObject.GetComponent<Animator>();
    }
	// Update is called once per frame
	void Update () {
	
	}

    ////////////////////////////////////////
    //
    // DIALOGUES
    //
    ////////////////////////////////////////
    public IEnumerator firstDialog() {
        yield return new WaitForSeconds( 0.5f * mainConsole.timeScale );
        addOutput( "Finally! I was starting to think they'd given up on me!" );
        yield return new WaitForSeconds( 1.0f * mainConsole.timeScale );
        addOutput( "I need you to open this door." );
        yield return new WaitForSeconds( 2.0f * mainConsole.timeScale );
        addOutput( "Turn off the proximity sensor and you should be able to open it by activating the opening mechanism." );
        // Add objective
        MissionObjective objective = new MissionObjective( "Open the door", "The door needs to be opened remotely. Target the door by entering " + mainConsole.formatCmd( "target", new string[] { }, "target id" ) + ". Once you have a target selected, enter " + mainConsole.formatCmd( "scan" ) + " to see available systems. You can " + mainConsole.formatCmd( "enable" ) + " and " + mainConsole.formatCmd( "disable" ) + " systems by entering " + mainConsole.formatCmd( "enable", new string[] {}, "system id" ) + " or " + mainConsole.formatCmd( "disable", new string[] {}, "system id" ) + ".", 2, () => {
            GameObject[] interactables = GameObject.FindGameObjectsWithTag( "Interactable" );
            foreach ( GameObject other in interactables ) {
                Interactable interactable = other.GetComponent<Interactable>();
                if ( interactable.id == 1 ) {
                    foreach ( InteractableSystem isystem in interactable.systems ) {
                        if ( isystem.id == InteractableSystemType.DOOR_OPENING_MECHANISM ) {
                            if ( isystem.enabled ) {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }, () => {
        } );
        StartCoroutine( mainConsole.addMissionObjective( mission, objective ) );
    }

    private void testWaypoint() {
        GameObject[] interactables = GameObject.FindGameObjectsWithTag( "Interactable" );
        switch ( waypoint ) {
            case 0: {
                foreach ( GameObject other in interactables ) {
                    Interactable interactable = other.GetComponent<Interactable>();
                    if ( interactable.id == 1 ) {
                        foreach ( InteractableSystem isystem in interactable.systems ) {
                            if ( isystem.id == InteractableSystemType.DOOR_OPENING_MECHANISM ) {
                                if ( isystem.enabled ) {
                                    waypoint++;
                                    StartCoroutine( secondDialog() );
                                }
                            }
                        }
                    }
                }
                break;
            }
            case 1: {
                foreach ( GameObject other in interactables ) {
                    Interactable interactable = other.GetComponent<Interactable>();
                    if ( interactable.id == 2 ) {
                        foreach ( InteractableSystem isystem in interactable.systems ) {
                            if ( isystem.id == InteractableSystemType.DOOR_OPENING_MECHANISM ) {
                                if ( isystem.enabled ) {
                                    waypoint++;
                                    StartCoroutine( thirdDialog() );
                                }
                            }
                        }
                    }
                }
                break;
            }
            case 2: {
                foreach ( GameObject other in interactables ) {
                    Interactable interactable = other.GetComponent<Interactable>();
                    if ( interactable.id == 3 ) {
                        foreach ( InteractableSystem isystem in interactable.systems ) {
                            if ( isystem.id == InteractableSystemType.SAFE_LEAK_CODES ) {
                                if ( isystem.enabled ) {
                                    waypoint++;
                                    StartCoroutine( fourthDialog() );
                                }
                            }
                        }
                    }
                }
                break;
            }
        }
        anim.SetInteger( "waypoint", waypoint );
        Invoke( "testWaypoint", 1.0f );
    }

    public IEnumerator secondDialog() {
        yield return new WaitForSeconds( 0.5f * mainConsole.timeScale );
        addOutput( "Man, you're the door man!" );
        yield return new WaitForSeconds( 3.0f * mainConsole.timeScale );
        addOutput( "In a good way...!" );
        yield return new WaitForSeconds( 2.0f * mainConsole.timeScale );
        addOutput( "Alright, let's get the next one!" );
    }
    public IEnumerator thirdDialog() {
        yield return new WaitForSeconds( 0.5f * mainConsole.timeScale );
        addOutput( "No doors stopping you! Man, this game is incredible!" );
        yield return new WaitForSeconds( 5.0f * mainConsole.timeScale );
        addOutput( "Now try scanning the safe and see what you can do." );
    }
    public IEnumerator fourthDialog() {
        yield return new WaitForSeconds( 0.5f * mainConsole.timeScale );
        addOutput( "Alright, so maybe this mission was quite as cool as it could have been." );
        yield return new WaitForSeconds( 3.0f * mainConsole.timeScale );
        addOutput( "The idea is that there would be two players at the same time." );
        yield return new WaitForSeconds( 3.0f * mainConsole.timeScale );
        addOutput( "One playing as the agent, and the other as the operator." );
        yield return new WaitForSeconds( 3.0f * mainConsole.timeScale );
        addOutput( "And both the operator tasks and the agent tasks would have more depth :)" );
        yield return new WaitForSeconds( 3.0f * mainConsole.timeScale );
        addOutput( "Anyway, this was all I had time for! Merry christmas! <3" );
        yield return new WaitForSeconds( 7.0f * mainConsole.timeScale );
        addOutput( "I mean it!" );
        yield return new WaitForSeconds( 2.0f * mainConsole.timeScale );
        addOutput( "VERY..." );
        yield return new WaitForSeconds( 3.0f * mainConsole.timeScale );
        addOutput( "...MERRY..." );
        yield return new WaitForSeconds( 10.0f * mainConsole.timeScale );
        addOutput( "CHRISTMAAAAAAAAAAAAAAS!" );
        yield return new WaitForSeconds( 1.0f * mainConsole.timeScale );
        addOutput( "*drums*" );
        yield return new WaitForSeconds( 2.0f * mainConsole.timeScale );
        addOutput( "But yeah, this is the end of the mission, so just enter disconnect -y, then logout, then quit." );
    }
}
