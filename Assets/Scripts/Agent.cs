using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Agent : MonoBehaviour {

    public GUIStyle chatBackgroundStyle;
    private GUIStyle consoleFontStyle;
    private List<string> outputHistory;
    private int currentlyVisibleLine = 0;
    public ConsoleGUI mainConsole = null;
    public float lineAddTime = 0.01f;
    private Transform lineFeedSound;
    public float textMargin = 10.0f;

    [HideInInspector]
    public int agentId;
    [HideInInspector]
    public Mission mission;

    private int waypoint = 0;
    private Animator anim;

	// Use this for initialization
	void Start () {
        //Random.seed = 10;//;( int )System.DateTime.Now.Ticks + this.GetInstanceID();
        outputHistory = new List<string>();
        lineFeedSound = transform.Find( "Line feed sound" );
        GameObject consoleGameObject = GameObject.FindWithTag( "Player" ) as GameObject;
        if ( consoleGameObject != null ) {
            ConsoleGUI console = consoleGameObject.GetComponent<ConsoleGUI>();
            console.initAgent();
        }
        anim = gameObject.GetComponent<Animator>();
        Invoke( "testWaypoint", 1.0f );
	}

    public void setFromMainConsole() {
        this.lineAddTime = mainConsole.lineAddTime;
        this.consoleFontStyle = mainConsole.consoleFontStyle;
        this.agentId = mainConsole.currentAgentId;
        this.mission = mainConsole.currentMission;
        Invoke( "showNextLine", lineAddTime );
        StartCoroutine( firstDialog() );
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
    }

    private void testWaypoint() {
        GameObject[] interactables = GameObject.FindGameObjectsWithTag( "Interactable" );
        Debug.Log( interactables );
        switch ( waypoint ) {
            case 0: {
                foreach ( GameObject other in interactables ) {
                    Interactable interactable = other.GetComponent<Interactable>();
                    Debug.Log( interactable );
                    if ( interactable.id == 1 ) {
                        Debug.Log( "1" );
                        foreach ( InteractableSystem isystem in interactable.systems ) {
                            Debug.Log( "2" );
                            if ( isystem.id == InteractableSystemType.DOOR_OPENING_MECHANISM ) {
                                Debug.Log( "3" );
                                if ( isystem.enabled ) {
                                    Debug.Log( "4" );
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

    ////////////////////////////////////////
    //
    // OUTPUT AND RENDERING
    //
    ////////////////////////////////////////
    void showNextLine() {
        if ( currentlyVisibleLine > 0 ) {
            currentlyVisibleLine--;
            playLineFeedSound();
        }
        Invoke( "showNextLine", lineAddTime );
    }

    void addOutput( string output ) {
        output = "Agent #" + agentId + ": " + output;
        string[] lines = output.Split( new string[] { "\n" }, System.StringSplitOptions.None );
        // Only split the lines if we need to - this way we can keep track of
        // already visible lines and make things like progress bars.
        if ( lines.Length > 1 ) {
            foreach ( string line in lines ) {
                outputHistory.Insert( 0, line );
                currentlyVisibleLine++;
            }
        } else {
            outputHistory.Insert( 0, output );
            currentlyVisibleLine++;
        }
    }
    void playLineFeedSound() {
        if ( mainConsole.lineFeedSoundEnabled ) {
            lineFeedSound.audio.Play();
        }
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI() {
        if ( mainConsole != null ) {
            // Map header
            GUI.Box( new Rect( Screen.width * 0.7f, Screen.height * 0.04f - textMargin, Screen.width * 0.3f, 20.0f ), new GUIContent( "Map:" ), consoleFontStyle );

            // Comms header
            GUI.Box( new Rect( Screen.width * 0.7f, Screen.height * 0.7f - textMargin, Screen.width * 0.3f, 20.0f ), new GUIContent( "Agent communication:" ), consoleFontStyle );

            // Background
            GUI.Box( new Rect( Screen.width * 0.7f, Screen.height * 0.7f, Screen.width * 0.3f, Screen.height * 0.3f ), new GUIContent( "" ), chatBackgroundStyle );

            // Comms
            string output = "";
            for ( int i = outputHistory.Count - 1; i >= currentlyVisibleLine; i-- ) {
                output += "\n" + outputHistory[ i ];
            }
            GUI.Box( new Rect( Screen.width * 0.7f, Screen.height * 0.7f, Screen.width * 0.3f, Screen.height * 0.3f ), new GUIContent( output ), consoleFontStyle );
        }
    }
}
