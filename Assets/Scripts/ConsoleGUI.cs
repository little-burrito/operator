using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ConsoleGUI : MonoBehaviour {

    private List<Command> commandHistory;
    private List<string> outputHistory;
    private string currentInput;
    private string currentInputBackup;
    private int currentSelectedCommandHistory;
    private int currentMarkerPosition;
    private int currentlyVisibleLine = 0;

    public bool loggedIn = false;

    private List<Mission> missions;

    public GUIStyle consoleFontStyle;
    public GUIStyle consoleBackgroundStyle;
    public GUIStyle consoleBackgroundStyleConnected;

    private bool cursorBlink = false;
    public float cursorBlinkTime = 0.5f;

    public float lineAddTime = 0.01f;
    public float missionCompletionTestInterval = 1.0f;

    private Transform lineFeedSound;

    private bool isLoggingIn = false;

    [HideInInspector]
    public bool lineFeedSoundEnabled = true;

    public float timeScale = 1.0f;

    private static ConsoleGUI _instance;

    public bool connected = false;

    public int currentAgentId = 0;
    public Mission currentMission;

    private Interactable target = null;

    ////////////////////////////////////////
    //
    // SETUP
    //
    ////////////////////////////////////////
	// Use this for initialization
	void Start () {
        //Random.seed = ( int )System.DateTime.Now.Ticks + this.GetInstanceID();
        commandHistory = new List<Command>();
        outputHistory = new List<string>();
        missions = new List<Mission>();
        currentInput = currentInputBackup = "";
        currentMarkerPosition = 0;
        currentSelectedCommandHistory = -1;
        cursorBlink = true;
        lineFeedSound = transform.Find( "Line feed sound" );
        StartCoroutine( displayWelcomeMessage() );
        Invoke( "blinkCursor", cursorBlinkTime );
        Invoke( "showNextLine", lineAddTime );
        Invoke( "testMissionCompletion", missionCompletionTestInterval );
	}

    public static ConsoleGUI instance {
        get {
            if ( _instance == null ) {
                _instance = GameObject.FindObjectOfType<ConsoleGUI>();

                //Tell unity not to destroy this object when loading a new scene!
                DontDestroyOnLoad( _instance.gameObject );
            }

            return _instance;
        }
    }

    void Awake() {
        if ( _instance == null ) {
            //If I am the first instance, make me the Singleton
            _instance = this;
            DontDestroyOnLoad( this );
            initAgent();
        } else {
            //If a Singleton already exists and you find
            //another reference in scene, destroy it!
            if ( this != _instance ) {
                Destroy( this.gameObject );
            }
        }
    }

    ////////////////////////////////////////
    //
    // CUTSCENES
    //
    ////////////////////////////////////////
    IEnumerator displayWelcomeMessage() {
        isLoggingIn = true;
        addOutput( "Logging in..." );
        yield return new WaitForSeconds( 2.0f * timeScale );
        addOutput( "Username: **********" );
        yield return new WaitForSeconds( 0.5f * timeScale );
        addOutput( "First password: ************************" );
        yield return new WaitForSeconds( 0.5f * timeScale );
        addOutput( "Second password: ******************************" );
        yield return new WaitForSeconds( 1.0f * timeScale );
        string processingString = "Processing...";
        addOutput( processingString );
        yield return new WaitForSeconds( 3.0f * timeScale );
        addOutput( "Digital signature verified" );
        yield return new WaitForSeconds( 1.5f * timeScale );
        addOutput( "\nWelcome, Operator." );
        loggedIn = true;
        isLoggingIn = false;
        yield return new WaitForSeconds( 1.0f * timeScale );
        addOutput( "Enter help to load the help file" );
        yield return new WaitForSeconds( 3.0f * timeScale );
        int missionId = generateNewMissionId();
        int agentId = generateNewAgentId();
        Mission mission = new Mission( "Seize launch codes", "Agent #" + agentId + " is in need of assistance. You need to connect to the agent and retrieve the launch codes. Connect to agent #" + agentId + " by entering connect agent " + agentId + " and take control of the situation.", missionId, agentId, "level1" );
        mission.addObjective( new MissionObjective( "Connect to the agent", "Connect to the agent by entering connect agent " + agentId, 1, () => {
            if ( Application.loadedLevelName == mission.scene ) {
                return true;
            }
            return false;
        }, () => { } ) );
        mission.addObjective( new MissionObjective( "Open the door", "The door needs to be opened remotely. Target the door by entering target <target id>. Once you have a target selected, enter scan to see available systems. You can enable and disable systems by entering enable <system id> or disable <system id>.", 2, () => {
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
        } ) );
        mission.addObjective( new MissionObjective( "Extract codes from the safe", "Get into the safe undiscovered and extract the codes from inside.", 3, () => {
            GameObject[] interactables = GameObject.FindGameObjectsWithTag( "Interactable" );
            foreach ( GameObject other in interactables ) {
                Interactable interactable = other.GetComponent<Interactable>();
                if ( interactable.id == 3 ) {
                    foreach ( InteractableSystem isystem in interactable.systems ) {
                        if ( isystem.id == InteractableSystemType.SAFE_LEAK_CODES ) {
                            if ( isystem.enabled ) {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }, () => { } ) );
        addMission( mission );
        //yield return new WaitForSeconds( 10.0f * timeScale );
        //MissionObjective objective = new MissionObjective( "Then what?", "This one was added later!", 4, mission );
        //addMissionObjective( mission, objective );
    }

    ////////////////////////////////////////
    //
    // HELPERS
    //
    ////////////////////////////////////////
    int generateNewMissionId() {
        bool collision = true;
        int missionId = 0;
        while ( collision ) {
            collision = false;
            missionId = Random.Range( 10000, 100000 );
            foreach ( Mission m in missions ) {
                if ( m.id == missionId ) {
                    collision = true;
                    break;
                }
            }
        }
        return missionId;
    }
    int generateNewAgentId() {
        bool collision = true;
        int agentId = 0;
        while ( collision ) {
            collision = false;
            agentId = Random.Range( 10000, 100000 );
            foreach ( Mission m in missions ) {
                if ( m.agentId == agentId ) {
                    collision = true;
                    break;
                }
            }
        }
        return agentId;
    }
	
    ////////////////////////////////////////
    //
    // INPUT
    //
    ////////////////////////////////////////
	void Update () {
        // Debug fix
        if ( commandHistory == null ) {
            commandHistory = new List<Command>();
        }

	    // Process input
        foreach ( char c in Input.inputString ) {
            if ( c == "\b"[ 0 ] ) {
                // Backspace
                if ( currentInput.Length != 0 ) {
                    if ( Input.GetKey( KeyCode.LeftControl ) || Input.GetKey( KeyCode.RightControl ) || Input.GetKey( KeyCode.LeftAlt ) || Input.GetKey( KeyCode.RightAlt ) ) {
                        // Remove full word
                        bool done = false;
                        if ( currentMarkerPosition > 0 ) {
                            if ( currentInput[ currentMarkerPosition - 1 ] == " "[ 0 ] ) {
                                done = true;
                            }
                            while ( !done ) {
                                if ( currentInput[ currentMarkerPosition - 1 ] == " "[ 0 ] ) {
                                    done = true;
                                    break;
                                }
                                if ( currentMarkerPosition > 0 ) {
                                    inputRemoveCharacter();
                                } else {
                                    done = true;
                                    break;
                                }
                            }
                            while ( currentMarkerPosition > 0 && currentInput[ currentMarkerPosition - 1 ] == " "[ 0 ] ) {
                                inputRemoveCharacter();
                            }
                        }
                    } else {
                        // Remove single character
                        inputRemoveCharacter();
                    }
                }
                resetCursorBlink();
            } else if ( c == "\n"[ 0 ] || c == "\r"[ 0 ] ) {
                // Enter
                if ( currentInput.Length > 0 ) {
                    Command command = new Command( currentInput );
                    execute( command );
                }
                resetCursorBlink();
            } else if ( ( int )c == 127 ) {
                // Remove full word
                bool done = false;
                if ( currentMarkerPosition > 0 ) {
                    if ( currentInput[ currentMarkerPosition - 1 ] == " "[ 0 ] ) {
                        done = true;
                    }
                    while ( !done ) {
                        if ( currentMarkerPosition > 0 ) {
                            if ( currentInput[ currentMarkerPosition - 1 ] == " "[ 0 ] ) {
                                done = true;
                                break;
                            }
                            inputRemoveCharacter();
                        } else {
                            done = true;
                            break;
                        }
                    }
                    while ( currentMarkerPosition > 0 && currentInput[ currentMarkerPosition - 1 ] == " "[ 0 ] ) {
                        inputRemoveCharacter();
                    }
                }
            } else {
                inputAddCharacter( c );
                resetCursorBlink();
            }
        }
        // Special keys
        if ( Input.GetKeyDown( KeyCode.LeftArrow ) ) {
            if ( !Input.GetKey( KeyCode.LeftControl ) && !Input.GetKey( KeyCode.RightControl ) && !Input.GetKey( KeyCode.LeftAlt ) && !Input.GetKey( KeyCode.RightAlt ) ) {
                if ( currentMarkerPosition > 0 ) {
                    currentMarkerPosition--;
                }
            } else {
                bool done = false;
                if ( currentMarkerPosition > 0 ) {
                    if ( currentInput[ currentMarkerPosition - 1 ] == " "[ 0 ] ) {
                        done = true;
                    }
                    while ( !done ) {
                        if ( currentMarkerPosition > 0 ) {
                            if ( currentInput[ currentMarkerPosition - 1 ] == " "[ 0 ] ) {
                                done = true;
                                break;
                            }
                            currentMarkerPosition--;
                        } else {
                            done = true;
                            break;
                        }
                    }
                    while ( currentMarkerPosition > 0 && currentInput[ currentMarkerPosition - 1 ] == " "[ 0 ] ) {
                        currentMarkerPosition--;
                    }
                }
            }
            resetCursorBlink();
        }
        if ( Input.GetKeyDown( KeyCode.RightArrow ) ) {
            if ( !Input.GetKey( KeyCode.LeftControl ) && !Input.GetKey( KeyCode.RightControl ) && !Input.GetKey( KeyCode.LeftAlt ) && !Input.GetKey( KeyCode.RightAlt ) ) {
                if ( currentMarkerPosition < currentInput.Length ) {
                    currentMarkerPosition++;
                }
            } else {
                bool done = false;
                if ( currentMarkerPosition < currentInput.Length - 1 ) {
                    if ( currentInput[ currentMarkerPosition ] == " "[ 0 ] ) {
                        done = true;
                    }
                    while ( !done ) {
                        if ( currentMarkerPosition < currentInput.Length ) {
                            if ( currentInput[ currentMarkerPosition ] == " "[ 0 ] ) {
                                done = true;
                                break;
                            }
                            currentMarkerPosition++;
                        } else {
                            done = true;
                            break;
                        }
                    }
                    while ( currentMarkerPosition < currentInput.Length && currentInput[ currentMarkerPosition ] == " "[ 0 ] ) {
                        currentMarkerPosition++;
                    }
                }
            }
            resetCursorBlink();
        }
        if ( Input.GetKeyDown( KeyCode.UpArrow ) ) {
            loadCommandHistoryToInput( currentSelectedCommandHistory + 1 );
        }
        if ( Input.GetKeyDown( KeyCode.DownArrow ) ) {
            loadCommandHistoryToInput( currentSelectedCommandHistory - 1 );
        }
        if ( Input.GetKeyDown( KeyCode.Delete ) ) {
            if ( !Input.GetKey( KeyCode.LeftControl ) && !Input.GetKey( KeyCode.RightControl ) && !Input.GetKey( KeyCode.LeftAlt ) && !Input.GetKey( KeyCode.RightAlt ) ) {
                inputRemoveCharacterAfter();
            } else {
                bool done = false;
                if ( currentMarkerPosition < currentInput.Length ) {
                    if ( currentInput[ currentMarkerPosition ] == " "[ 0 ] ) {
                        done = true;
                    }
                    while ( !done ) {
                        if ( currentMarkerPosition < currentInput.Length ) {
                            if ( currentInput[ currentMarkerPosition ] == " "[ 0 ] ) {
                                done = true;
                                break;
                            }
                            inputRemoveCharacterAfter();
                        } else {
                            done = true;
                            break;
                        }
                    }
                    while ( currentMarkerPosition < currentInput.Length - 1 && currentInput[ currentMarkerPosition ] == " "[ 0 ] ) {
                        inputRemoveCharacterAfter();
                    }
                }
            }
            resetCursorBlink();
        }
        if ( Input.GetKeyDown( KeyCode.Home ) ) {
            currentMarkerPosition = 0;
            resetCursorBlink();
        }
        if ( Input.GetKeyDown( KeyCode.End ) ) {
            currentMarkerPosition = currentInput.Length;
            resetCursorBlink();
        }

        // Set input backup unless we're editing a previous command
        updateInputBackup();
	}

    void inputRemoveCharacter() {
        if ( currentMarkerPosition > 0 ) {
            string buffer = currentInput;
            currentInput = buffer.Substring( 0, currentMarkerPosition - 1 );
            if ( currentMarkerPosition < buffer.Length ) {
                currentInput += buffer.Substring( currentMarkerPosition, buffer.Length - currentMarkerPosition );
            }
            currentMarkerPosition--;
        }
        resetCursorBlink();
    }
    void inputRemoveCharacterAfter() {
        if ( currentMarkerPosition < currentInput.Length ) {
            string buffer = currentInput;
            currentInput = buffer.Substring( 0, currentMarkerPosition - 1 + 1 );
            if ( currentMarkerPosition + 1 < buffer.Length ) {
                currentInput += buffer.Substring( currentMarkerPosition + 1, buffer.Length - currentMarkerPosition - 1 );
            }
        }
        resetCursorBlink();
    }
    void inputAddCharacter( char c ) {
        // Other characters
        string buffer = currentInput;
        currentInput = buffer.Substring( 0, currentMarkerPosition );
        currentInput += c;
        currentMarkerPosition++;
        if ( currentMarkerPosition < buffer.Length ) {
            currentInput += buffer.Substring( currentMarkerPosition - 1, buffer.Length - currentMarkerPosition + 1 );
        }
        resetCursorBlink();
    }
    string getInputWithCursorBlink() {
        string returnValue, buffer = currentInput;
        string blink = "█";
        if ( !cursorBlink ) {
            blink = " ";
        }
        if ( currentMarkerPosition > 0 ) {
            returnValue = buffer.Substring( 0, currentMarkerPosition );
            returnValue += blink;
            if ( currentMarkerPosition + 1 < buffer.Length + 1 ) {
                returnValue += buffer.Substring( currentMarkerPosition, buffer.Length - currentMarkerPosition );
            }
        } else {
            returnValue = blink + buffer;
        }
        return returnValue;
    }

    void loadCommandHistoryToInput( int commandHistoryIndex ) {
        if ( currentSelectedCommandHistory == -1 ) {
            currentInputBackup = currentInput;
        }

        if ( commandHistoryIndex > -1 && commandHistoryIndex < commandHistory.Count ) {
            currentSelectedCommandHistory = commandHistoryIndex;
            currentInput = commandHistory[ commandHistoryIndex ].fullCommandString;
        } else if ( commandHistoryIndex == -1 ) {
            currentSelectedCommandHistory = commandHistoryIndex;
            currentInput = currentInputBackup;
        } else if ( commandHistoryIndex == -2 ) {
            currentSelectedCommandHistory = -1;
            currentInput = currentInputBackup = "";
        }
        currentMarkerPosition = currentInput.Length;
        resetCursorBlink();
    }

    void updateInputBackup( ) {
        if ( currentSelectedCommandHistory == -1 ) {
            currentInputBackup = currentInput;
        }
    }

    void execute( Command command ) {
        commandHistory.Insert( 0, command );
        addOutput( "" );
        addOutput( "> " + command.fullCommandString );
        currentInput = currentInputBackup = ""; // Will this work? :)
        currentMarkerPosition = 0;
        currentSelectedCommandHistory = -1;
        runCommand( command );
    }

    ////////////////////////////////////////
    //
    // COMMAND DELEGATION
    //
    ////////////////////////////////////////
    void runCommand( Command cmd ) {
        if ( loggedIn ) {
            switch ( cmd.command ) {
                case "target": {
                        StartCoroutine( cmdTarget( cmd ) );
                        break;
                    }
                case "scan": {
                        StartCoroutine( cmdScan( cmd ) );
                        break;
                    }
                case "accept": {
                        cmdAccept( cmd );
                        break;
                    }
                case "en":
                case "enable": {
                        StartCoroutine( cmdEnable( cmd ) );
                        break;
                    }
                case "dis":
                case "disable": {
                        StartCoroutine( cmdDisable( cmd ) );
                        break;
                    }
                case "conn":
                case "connect": {
                        StartCoroutine( cmdConnect( cmd ) );
                        break;
                    }
                case "disc":
                case "disconnect": {
                        StartCoroutine( cmdDisconnect( cmd ) );
                        break;
                    }
                case "options":
                case "opt": {
                    cmdOptions( cmd );
                    break;
                    }
                case "login":
                case "logon": {
                    addOutput( "You are already logged in." );
                    break;
                    }
                case "logout":
                case "logoff": {
                    if ( connected ) {
                        execute( new Command( "disconnect -y" ) );
                    }
                        StartCoroutine( cmdLogout( cmd ) );
                        break;
                    }
                case "list":
                case "dir":
                case "ls": {
                        cmdList( cmd );
                        break;
                    }
                case "exit":
                case "quit": {
                        addOutput( "Please don't quit without logging out first!" );
                        break;
                    }
                case "help":
                case "?":
                case "h": {
                        outputHelpFile( cmd );
                        break;
                    }
                case "restart":
                case "r": {
                        cmdRestart( cmd );
                        break;
                    }
                case "clear":
                case "cls": {
                        if ( cmd.parameters.Count == 0 ) {
                            outputHistory = new List<string>();
                        } else {
                            outputInvalidFormat( cmd, "clear" );
                        }
                        break;
                    }
                default: {
                        outputInvalidCommand( cmd );
                        break;
                    }
            }
        } else {
            switch ( cmd.command ) {
                case "login":
                case "logon": {
                    cmdLogin( cmd );
                    break;
                }
                case "quit":
                case "exit": {
                        Application.Quit();
                        addOutput( "I'm just messing with you! There is no quit :)" );
                        break;
                    }
                case "options":
                case "opt": {
                    cmdOptions( cmd );
                    break;
                    }
                default: {
                    addOutput( "You are not logged in. Log in using command: login" );
                    break;
                }
            }
        }
    }

    ////////////////////////////////////////
    //
    // BEHAVIOURS
    //
    ////////////////////////////////////////
    void OnLevelWasLoaded( int level ) {
        initAgent();
    }
    public void initAgent() {
        GameObject agentGameObject = GameObject.FindWithTag( "Agent" ) as GameObject;
        if ( agentGameObject != null ) {
            Agent agent = agentGameObject.GetComponent<Agent>();
            if ( agent.mainConsole == null ) {
                agent.mainConsole = this;
                agent.setFromMainConsole();
            }
        }
    }

    ////////////////////////////////////////
    //
    // OUTPUT
    //
    ////////////////////////////////////////
    void addOutput( string output ) {
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
    void addOutputWithSpacing( string output ) {
        addOutput( "" );
        addOutput( output );
    }
    void outputInvalidFormat( Command cmd, string expectedFormat ) {
        addOutput( "Command \"" + cmd.command + "\" invalid format. Expected:\n" + expectedFormat );
    }
    void outputInvalidCommand( Command cmd ) {
        addOutput( "ERROR: Unknown command \"" + cmd.command + "\"" );
    }
    void outputHelp( Command cmd, string help ) {
        addOutput( "Help file for command \"" + cmd.command + "\"\n" + help );
    }
    void outputCommandNotYetImplemented( Command cmd ) {
        addOutput( "This command has not yet been implemented." );
    }
    void outputHelpFile( Command cmd ) {
        addOutput( "=== HELP FILE ===" );
        addOutput( "" );
        addOutput( "General instructions:" );
        addOutput( "You control everything using your console. If" );
        addOutput( "you want more information about a specific" );
        addOutput( "command, or a shorthand for it's parameters," );
        addOutput( "you can enter <command> -help" );
        addOutput( "" );
        addOutput( "List of available commands:" );
        addOutput( "" );
        addOutput( "Starting and ending" );
        addOutput( " connect (conn) - connects to an agent" );
        addOutput( " disconnect (disc) - disconnects from an agent" );
        addOutput( "Actions" );
        addOutput( " list (ls) - displays a list of the selected content" );
        addOutput( " accept mission - accept a mission" );
        addOutput( " target - targets an object" );
        addOutput( " scan - scans the targeted object" );
        addOutput( " enable(en) - enables a system on the targeted object" );
        addOutput( " disable(dis) - disables a system on the targeted object" );
        addOutput( "Extra commands" );
        addOutput( " help (h) (?) - this help file" );
        addOutput( " clear (cls) - clear the screen from text" );
        addOutput( " options (opt) - edit options" );
        addOutput( " login - logs you in" );
        addOutput( " logout - logs you out" );
        addOutput( " restart (r) - restart the game" );
        addOutput( "" );
        addOutput( "To view currently available missions, enter list missions" );
    }

    ////////////////////////////////////////
    //
    // RENDERING
    //
    ////////////////////////////////////////
    void showNextLine() {
        if ( currentlyVisibleLine > 0 ) {
            currentlyVisibleLine--;
            playLineFeedSound();
        }
        Invoke( "showNextLine", lineAddTime );
    }
    void blinkCursor() {
        cursorBlink = !cursorBlink;
        Invoke( "blinkCursor", cursorBlinkTime );
    }
    void resetCursorBlink() {
        cursorBlink = true;
        if ( IsInvoking( "blinkCursor" ) ) {
            CancelInvoke( "blinkCursor" );
        }
        Invoke( "blinkCursor", cursorBlinkTime );
    }
    void OnGUI() {
        // Background
        if ( connected ) {
            GUI.Box( new Rect( 0, 0, Screen.width, Screen.height ), new GUIContent( "" ), consoleBackgroundStyleConnected );
        } else {
            GUI.Box( new Rect( 0, 0, Screen.width, Screen.height ), new GUIContent( "" ), consoleBackgroundStyle );
        }

        string output = "";
        for ( int i = outputHistory.Count - 1; i >= currentlyVisibleLine; i-- ) {
            output += outputHistory[ i ] + "\n";
        }
        output += "\n> " + getInputWithCursorBlink();
        //output += currentInput;
        GUI.Box( new Rect( 0, 0, Screen.width / 2, Screen.height ), new GUIContent( output ), consoleFontStyle );
    }
    ////////////////////////////////////////
    //
    // SOUNDS
    //
    ////////////////////////////////////////
    void playLineFeedSound() {
        if ( lineFeedSoundEnabled ) {
            lineFeedSound.audio.Play();
        }
    }

    ////////////////////////////////////////
    //
    // MISSIONS
    //
    ////////////////////////////////////////
    public void addMission( Mission mission ) {
        missions.Add( mission );
        addOutputWithSpacing( "*** New mission received - #" + mission.id + ": " + mission.title + "\nEnter list mission " + mission.id + " for more info" );
    }
    public void addMissionObjective( Mission mission, MissionObjective objective ) {
        foreach ( Mission m in missions ) {
            if ( m.id == mission.id ) {
                m.addObjective( objective );
                addOutputWithSpacing( "*** New objective received: " + objective.title + "\nEnter list mission " + m.id + " for more details." );
                break;
            }
        }
    }
    void outputMissionComplete( Mission m ) {
        addOutputWithSpacing( "Mission completed. #" + m.id + ": " + m.title
                            + "\nEnter disconnect -y to give control back to the agent and return to the lobby." );
    }
    void outputObjectiveComplete( Mission m, MissionObjective o ) {
        addOutputWithSpacing( "Objective " + o.title + " completed. Enter list mission " + m.id + " for a list of objectives" );
        o.runCompletionFunction();
    }
    void outputMissionInList( Mission m ) {
        addOutput( "#" + m.id + ": " + m.title + ( m.accepted ? " - Agent #" + m.agentId : " - Enter accept mission " + m.id + " to accept or list mission " + m.id + " for more information." ) );
            //+ "\nAccepted: [" + ( m.accepted ? "X" : " " ) + "]" 
            //+ " Completed: [" + ( m.completed ? "X" : " " ) + "]" );
    }
    void outputMission( Mission m ) {
        string status;
        if ( m.completed ) {
            status = "COMPLETED";
        } else if ( m.accepted ) {
            status = "ACTIVE";
        } else {
            status = "PENDING ACCEPTANCE";
        }
        addOutput( "MISSION #" + m.id + " - " + m.title
            + ( m.accepted ? "" : "\nEnter accept mission " + m.id + " to accept." ) 
            + "\nSTATUS: " + status
            + "\nAGENT: #" + m.agentId
            + "\n\n" + "MISSION BRIEFING"
            //+ "\nAccepted: [" + ( m.accepted ? "X" : " " ) + "]"
            //+ " Completed: [" + ( m.completed ? "X" : " " ) + "]"
            + "\n" + m.description + "\n\nOBJECTIVES" );

        bool first = true;
        foreach ( MissionObjective o in m.objectives ) {
            if ( !first ) {
                addOutput( "" );
            }
            first = false;
            addOutput( o.title + " - " + ( o.completed ? "" : "NOT " ) + "COMPLETED"
                + "\n" + o.description );
        }
    }
    void testMissionCompletion() {
        foreach ( Mission m in missions ) {
            if ( !m.completed && m.accepted ) {
                foreach ( MissionObjective o in m.objectives ) {
                    if ( !o.completed ) {
                        if ( o.completeObjective() ) {
                            outputObjectiveComplete( m, o );
                        }
                    }
                }
                if ( m.completeMission() ) {
                    outputMissionComplete( m );
                }
            }
        }
        Invoke( "testMissionCompletion", missionCompletionTestInterval );
    }

    ////////////////////////////////////////
    //
    // COMMANDS
    //
    ////////////////////////////////////////
    bool isHelpParameter( string parameter ) {
        switch ( parameter ) {
            case "-?":
            case "/?":
            case "-h":
            case "/h":
            case "-help":
            case "/help": {
                return true;
                }
        }
        return false;
    }
    bool shouldDisplayHelpNoParameter( Command cmd ) {
        bool displayHelp = false;
        if ( cmd.parameters.Count == 0 ) {
            displayHelp = false;
        } else if ( isHelpParameter( cmd.parameters[ 0 ] ) ) {
            displayHelp = true;
        }
        return displayHelp;
    }
    bool shouldDisplayHelp( Command cmd ) {
        bool displayHelp = false;
        if ( cmd.parameters.Count == 0 ) {
            displayHelp = true;
        } else if ( isHelpParameter( cmd.parameters[ 0 ] ) ) {
            displayHelp = true;
        }
        return displayHelp;
    }

    // Restart
    void cmdRestart( Command cmd ) {
        if ( shouldDisplayHelp( cmd ) ) {
            outputHelp( cmd, "restart -y - restarts the game" );
        } else {
            if ( cmd.parameters[ 0 ] == "-y" ) {
                Application.LoadLevel( Application.loadedLevel );
            } else {
                outputInvalidFormat( cmd, "restart -y" );
            }
        }
    }

    // Connect
    IEnumerator cmdConnect( Command cmd ) {
        if ( shouldDisplayHelp( cmd ) ) {
            outputHelp( cmd, "connect agent(a) <agent id #> - starts a session with the selected agent"
                            + "\nEnter list agents to see available agents" );
        } else {
            switch ( cmd.parameters[ 0 ] ) {
                case "a":
                case "agent": {
                    if ( cmd.parameters.Count > 1 ) {
                        int agentId;
                        if ( int.TryParse( cmd.parameters[ 1 ], out agentId ) ) {
                            bool validInput = false;
                            bool validAgent = false;
                            Mission mission = new Mission( "", "", 0, 0, "" );
                            foreach ( Mission m in missions ) {
                                if ( m.agentId == agentId ) {
                                    validInput = true;
                                    mission = m;
                                    if ( m.accepted && !m.completed ) {
                                        validAgent = true;
                                    }
                                    break;
                                }
                            }
                            if ( !validInput ) {
                                addOutput( "Agent #" + agentId + " not found" );
                            } else {
                                if ( validAgent ) {
                                    addOutput( "Establishing connection..." );
                                    yield return new WaitForSeconds( 2.0f * timeScale );
                                    addOutput( "Connection established" );
                                    yield return new WaitForSeconds( 0.5f * timeScale );
                                    addOutput( "Loading stream..." );
                                    yield return new WaitForSeconds( 0.5f * timeScale );
                                    addOutput( "Stream loaded" );
                                    currentMission = mission;
                                    currentAgentId = agentId;
                                    connected = true;
                                    Application.LoadLevel( mission.scene );
                                } else {
                                    addOutput( "The mission must be accepted before connecting to the agent."
                                            + "\nEnter accept mission " + mission.id + " to accept the mission." );
                                }
                            }
                        } else {
                                outputInvalidFormat( cmd, "connect agent(a) <agent id #> - starts a session with the selected agent" );
                            }
                    } else {
                        outputHelp( cmd, "connect agent(a) <agent id #> - starts a session with the selected agent"
                                        + "\nEnter list agents to see available agents" );
                    }
                    break;
                }
                default: {
                    outputHelp( cmd, "connect agent(a) <agent id #> - starts a session with the selected agent"
                                        + "\nEnter list agents to see available agents" );
                    break;
                    }
            }
        }
    }

    // Disconnect
    IEnumerator cmdDisconnect( Command cmd ) {
        if ( shouldDisplayHelp( cmd ) ) {
            outputHelp( cmd, "disconnect -y - disconnects from the current agent" );
        } else {
            if ( cmd.parameters[ 0 ] == "-y" ) {
                addOutput( "Closing connection..." );
                yield return new WaitForSeconds( 1.0f * timeScale );
                addOutput( "Connection succesfully closed" );
                connected = false;
                Application.LoadLevel( "main" );
            } else {
                outputInvalidFormat( cmd, "disconnect -y" );
            }
        }
    }

    // List
    void cmdList( Command cmd ) {
        if ( shouldDisplayHelp( cmd ) ) {
            outputHelp( cmd, "list missions(miss) - lists currently available missions"
                             + "\nlist mission(m) <mission id #> - display details for the selected mission"
                             + "\nlist agents(a) - lists available agents"
                             + "\nlist systems(s) - lists systems of targeted object" );
        } else {
            switch ( cmd.parameters[ 0 ] ) {
                case "miss":
                case "missions": {
                    bool listNotEmpty = false;

                    // Not accepted mission
                    bool missionListed = false;
                    foreach ( Mission mission in missions ) {
                        if ( !mission.accepted ) {
                            if ( !missionListed ) {
                                addOutput( "PENDING MISSIONS" );
                                missionListed = true;
                                listNotEmpty = true;
                            }
                            outputMissionInList( mission );
                        }
                    }

                    // Active missions
                    missionListed = false;
                    foreach ( Mission mission in missions ) {
                        if ( mission.accepted && !mission.completed ) {
                            if ( !missionListed ) {
                                addOutput( "ACTIVE MISSIONS" );
                                missionListed = true;
                                listNotEmpty = true;
                            }
                            outputMissionInList( mission );
                        }
                    }

                    // Completed missions
                    missionListed = false;
                    foreach ( Mission mission in missions ) {
                        if ( mission.completed ) {
                            if ( !missionListed ) {
                                addOutput( "COMPLETED MISSIONS" );
                                missionListed = true;
                                listNotEmpty = true;
                            }
                            outputMissionInList( mission );
                        }
                    }

                    if ( !listNotEmpty ) {
                        addOutput( "Your mission list is empty" );
                    }
                    break;
                }
                case "m":
                case "mission": {
                    if ( cmd.parameters.Count == 2 ) {
                        int missionId;
                        if ( int.TryParse( cmd.parameters[ 1 ], out missionId ) ) {
                            bool missionListed = false;
                            foreach ( Mission m in missions ) {
                                if ( m.id == missionId ) {
                                    outputMission( m );
                                    missionListed = true;
                                    break;
                                }
                            }
                            if ( !missionListed ) {
                                addOutput( "Unable to select mission " + missionId + ". List missions to see available missions." );
                            }
                        } else {
                            outputInvalidFormat( cmd, "list mission <mission id #> - display details for the selected mission" );
                        }
                    } else {
                        outputInvalidFormat( cmd, "list mission <mission id #> - display details for the selected mission" );
                    }
                    break;
                }
                case "a":
                case "agents": {
                    bool listNotEmpty = false;
                    bool didOutput = false;
                    foreach ( Mission m in missions ) {
                        if ( m.accepted && !m.completed ) {
                            if ( !didOutput ) {
                                addOutput( "AVAILABLE AGENTS (ACTIVE MISSIONS):" );
                                didOutput = true;
                                listNotEmpty = true;
                            }
                            addOutput( "Agent #" + m.agentId + " for mission #" + m.id + ": " + m.title );
                        }
                    }

                    didOutput = false;
                    foreach ( Mission m in missions ) {
                        if ( !m.accepted ) {
                            if ( !didOutput ) {
                                addOutput( "UNAVAILABLE AGENTS (PENDING MISSIONS):" );
                                didOutput = true;
                                listNotEmpty = true;
                            }
                            addOutput( "Agent #" + m.agentId + " for mission #" + m.id + ": " + m.title );
                            didOutput = true;
                        }
                    }

                    if ( !listNotEmpty ) {
                        addOutput( "Your agent list is empty" );
                    }

                    break;
                }
                case "s":
                case "systems": {
                    outputCommandNotYetImplemented( cmd );
                    break;
                }
                default: {
                    outputInvalidFormat( cmd, "list missions(miss) - lists currently available missions"
                             + "\nlist mission(m) <mission id #> - display details for the selected mission"
                             + "\nlist agents(a) - lists available agents"
                             + "\nlist systems(s) - lists systems of targeted object" );
                    break;
                }
            }
        }
    }

    // Target
    IEnumerator cmdTarget( Command cmd ) {
        if ( cmd.parameters.Count == 0 ) {
            if ( target == null ) {
                addOutput( "No target selected" );
            } else {
                addOutput( "Target: " + target.displayName );
            }
        } else if ( shouldDisplayHelp( cmd ) ) {
            outputHelp( cmd, "target - displays current target"
                        + "\ntarget <target id> - targets the selected object"
                        + "\ntarget deselect(des) - deselects the current target" );
        } else {
            switch ( cmd.parameters[ 0 ] ) {
                case "des":
                case "deselect": {
                    if ( target != null ) {
                        addOutput( "Deselecting target..." );
                        yield return new WaitForSeconds( 0.5f * timeScale );
                        target = null;
                        addOutput( "No target selected" );
                    } else {
                        addOutput( "No target selected" );
                    }
                    break;
                }
                default: {
                    if ( cmd.parameters.Count == 1 ) {
                        GameObject[] objects = GameObject.FindGameObjectsWithTag( "Interactable" );
                        Interactable interactable = null;
                        foreach ( GameObject obj in objects ) {
                            Interactable currentInteractable = obj.GetComponent<Interactable>();
                            if ( currentInteractable != null ) {
                                if ( currentInteractable.displayName.ToLower() == cmd.parameters[ 0 ] ) {
                                    interactable = currentInteractable;
                                    break;
                                }
                            }
                        }
                        if ( interactable != null ) {
                            addOutput( "Locking target..." );
                            yield return new WaitForSeconds( 1.0f * timeScale );
                            this.target = interactable;
                            addOutput( "Target " + target.displayName + " selected" );
                        } else {
                            addOutput( "Target not found: " + cmd.parameters[ 0 ] );
                        }
                    } else {
                        outputHelp( cmd, "target - displays current target"
                                    + "\ntarget <target id> - targets the selected object"
                                    + "\ntarget deselect(des) - deselects the current target" );
                    }
                    break;
                }
            }
        }
    }

    // Scan
    IEnumerator cmdScan( Command cmd ) {
        if ( shouldDisplayHelpNoParameter( cmd ) ) {
            outputHelp( cmd, "scan - scans the targeted object" );
        } else {
            if ( target != null ) {
                addOutput( "Scanning..." );
                foreach ( InteractableSystem isystem in target.systems ) {
                    yield return new WaitForSeconds( 0.1f * Random.Range( 1, 3 ) * timeScale );
                    addOutput( isystem.displayName + " - id: " + isystem.name + " - " + ( isystem.enabled ? "ENABLED" : "DISABLED" ) );
                }
            } else {
                addOutput( "No target selected" );
            }
        }
    }

    // Accept
    void cmdAccept( Command cmd ) {
        if ( shouldDisplayHelp( cmd ) ) {
            outputHelp( cmd, "accept mission(m) <mission id #> - accepts selected mission"
                             + "" );
        } else {
            switch ( cmd.parameters[ 0 ] ) {
                case "m":
                case "mission": {
                    if ( cmd.parameters.Count == 2 ) {
                        int missionId;
                        if ( int.TryParse( cmd.parameters[ 1 ], out missionId ) ) {
                            bool acceptedMission = false;
                            foreach ( Mission m in missions ) {
                                if ( m.id == missionId ) {
                                    m.accepted = true;
                                    addOutput( "Mission accepted" );
                                    acceptedMission = true;
                                    break;
                                }
                            }
                            if ( !acceptedMission ) {
                                addOutput( "Unable to accept mission " + missionId + ". List missions to see available missions" );
                            }
                        } else {
                            outputInvalidFormat( cmd, "list mission(m) <mission id #> - display selected mission" );
                        }
                    } else {
                        outputInvalidFormat( cmd, "accept mission(m) <mission id #> - accepts selected mission"
                             + "" );
                    }
                    break;
                }
                default: {
                    outputInvalidFormat( cmd, "accept mission(m) <mission id #> - accepts selected mission"
                             + "" );
                    break;
                }
            }
        }
    }

    // Log out
    IEnumerator cmdLogout( Command cmd ) {
        if ( shouldDisplayHelpNoParameter( cmd ) ) {
            outputHelp( cmd, "logout - logs you out of the system." );
        } else {
            if ( isLoggingIn ) {
                StopCoroutine( displayWelcomeMessage() );
            }
            addOutput( "Logging out..." );
            yield return new WaitForSeconds( 0.5f );
            addOutput( "User successfully logged out" );
            loggedIn = false;
        }
    }

    // Log in
    void cmdLogin( Command cmd ) {
        if ( !isLoggingIn ) {
            StartCoroutine( displayWelcomeMessage() );
        }
    }

    // Options
    void cmdOptions( Command cmd ) {
        if ( shouldDisplayHelp( cmd ) ) {
            outputHelp( cmd, "options sound - edit sound options"
                            + "\noptions timescale(ts) - changes the time scale, which is cheating" );
        } else {
            switch ( cmd.parameters[ 0 ] ) {
                case "sound": {
                        if ( cmd.parameters.Count > 1 ) {
                            switch ( cmd.parameters[ 1 ] ) {
                                case "linefeed": {
                                        if ( cmd.parameters.Count > 2 ) {
                                            if ( cmd.parameters[ 2 ] == "on" ) {
                                                lineFeedSoundEnabled = true;
                                                addOutput( "Line feed sound enabled" );
                                            } else if ( cmd.parameters[ 2 ] == "off" ) {
                                                lineFeedSoundEnabled = false;
                                                addOutput( "Line feed sound disabled" );
                                            } else {
                                                outputInvalidFormat( cmd, "options sound linefeed <on/off> - enable/disable line feed sound" );
                                            }
                                        } else {
                                            addOutput( "Line feed sound is " + ( lineFeedSoundEnabled ? "on" : "off" ) + "\nenter options sound linefeed <on/off> to enable/disable it" );
                                        }
                                        break;
                                    }
                                default: {
                                        outputHelp( cmd, "options sound linefeed <on/off> - enable/disable line feed sound" );
                                        break;
                                    }
                            }
                        } else {
                            outputHelp( cmd, "options sound linefeed - options for linefeed sound" );
                        }
                        break;
                    }
                case "ts":
                case "timescale": {
                        if ( cmd.parameters.Count == 2 ) {
                            float ts;
                            if ( float.TryParse( cmd.parameters[ 1 ], out ts ) ) {
                                timeScale = ts;
                                addOutput( "Time scale is now " + timeScale );
                            } else {
                                outputInvalidFormat( cmd, "options timescale <timescale> - set timescale to given value. 1 = Normal timescale, 0 = Everything is instant" );
                            }
                        } else if ( cmd.parameters.Count == 1 ) {
                            addOutput( "Time scale is " + timeScale );
                        } else {
                            outputInvalidFormat( cmd, "options timescale <timescale> - set timescale to given value. 1 = Normal timescale, 0 = Everything is instant" );
                        }
                        break;
                    }
                default: {
                    outputHelp( cmd, "options sound - edit sound options"
                            + "\noptions timescale(ts) - changes the time scale, which is cheating" );
                    break;
                    }

            }
        }
    }

    // Enable
    IEnumerator cmdEnable( Command cmd ) {
        if ( shouldDisplayHelp( cmd ) ) {
            outputHelp( cmd, "enable <system id> - enables the selected system" );
        } else {
            if ( target == null ) {
                addOutput( "No target selected" );
            } else {
                switch ( cmd.parameters[ 0 ] ) {
                    default: {
                            if ( cmd.parameters.Count == 1 ) {
                                bool found = false;
                                foreach ( InteractableSystem isystem in target.systems ) {
                                    if ( isystem.name.ToLower() == cmd.parameters[ 0 ] ) {
                                        found = true;
                                        addOutput( "Enabling system..." );
                                        yield return new WaitForSeconds( 0.5f * timeScale );
                                        isystem.activate();
                                        addOutput( isystem.response );
                                    }
                                }
                                if ( !found ) {
                                    addOutput( "System " + cmd.parameters[ 0 ] + " not found" );
                                }
                            } else {
                                outputHelp( cmd, "enable <system id> - enables the selected system" );
                            }
                            break;
                        }
                }
            }
        }
    }

    // Disable
    IEnumerator cmdDisable( Command cmd ) {
        if ( shouldDisplayHelp( cmd ) ) {
            outputHelp( cmd, "disable <system id> - disables the selected system" );
        } else {
            if ( target == null ) {
                addOutput( "No target selected" );
            } else {
                switch ( cmd.parameters[ 0 ] ) {
                    default: {
                            if ( cmd.parameters.Count == 1 ) {
                                bool found = false;
                                foreach ( InteractableSystem isystem in target.systems ) {
                                    if ( isystem.name.ToLower() == cmd.parameters[ 0 ] ) {
                                        found = true;
                                        addOutput( "Disabling system..." );
                                        yield return new WaitForSeconds( 0.5f * timeScale );
                                        isystem.deactivate();
                                        addOutput( isystem.response );
                                    }
                                }
                                if ( !found ) {
                                    addOutput( "System " + cmd.parameters[ 0 ] + " not found" );
                                }
                            } else {
                                outputHelp( cmd, "disable <system id> - disables the selected system" );
                            }
                            break;
                        }
                }
            }
        }
    }
}
