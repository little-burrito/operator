using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Agent : MonoBehaviour {

    public GUIStyle chatBackgroundStyle;
    public GUIStyle consoleFontStyle;
    public List<string> outputHistory;
    public int currentlyVisibleLine = 0;
    public ConsoleGUI mainConsole = null;
    public float lineAddTime = 0.01f;
    public Transform lineFeedSound;
    public float textMargin = 10.0f;

    [HideInInspector]
    public int agentId;
    [HideInInspector]
    public Mission mission;

    public delegate void Initialization();
    public Initialization init = null;

	// Use this for initialization
	void Start () {
        initializeAgent();
	}

    public void initializeAgent() {
        //Random.seed = 10;//;( int )System.DateTime.Now.Ticks + this.GetInstanceID();
        outputHistory = new List<string>();
        lineFeedSound = transform.Find( "Line feed sound" );
        GameObject consoleGameObject = GameObject.FindWithTag( "Player" ) as GameObject;
        if ( consoleGameObject != null ) {
            ConsoleGUI console = consoleGameObject.GetComponent<ConsoleGUI>();
            console.initAgent();
        }
    }

    public void setFromMainConsole() {
        this.lineAddTime = mainConsole.lineAddTime;
        this.consoleFontStyle = mainConsole.consoleFontStyle;
        this.agentId = mainConsole.currentAgentId;
        this.mission = mainConsole.currentMission;
        Invoke( "showNextLine", lineAddTime );
        init();
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

    public void addOutput( string output ) {
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
            consoleFontStyle.clipping = TextClipping.Clip;
            string output = "";
            for ( int i = outputHistory.Count - 1; i >= currentlyVisibleLine; i-- ) {
                output += "\n" + outputHistory[ i ];
            }
            GUI.Box( new Rect( Screen.width * 0.7f, Screen.height * 0.7f, Screen.width * 0.3f, Screen.height * 0.3f ), new GUIContent( output ), consoleFontStyle );
        }
    }
}
