using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ConsoleGUI : MonoBehaviour {

    private List<CommandInput> commandHistory;
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

    public Interactable target = null;

    public Command helpCommand;
    private List<Command> commands;

    private bool canAddMissionObjectives = true;

    ////////////////////////////////////////
    //
    // SETUP
    //
    ////////////////////////////////////////
	// Use this for initialization
    void Start() {
        //Random.seed = ( int )System.DateTime.Now.Ticks + this.GetInstanceID();
        commandHistory = new List<CommandInput>();
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
        currentMission = new Mission( "", "", -1, 0, "" );
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

        //////////////////
        // COMMANDS
        //////////////////
        helpCommand = new Command( "-help", new string[] { "-?", "/?", "-h", "/h", "/help" }, "Displays this help file.", this, ( CommandInput input ) => {
            /*CommandInput newInput = input;
            newInput.fullCommandString = newInput.fullCommandString.Replace( " -help", "" );
            newInput.fullCommandString = newInput.fullCommandString.Replace( " -?", "" );
            newInput.fullCommandString = newInput.fullCommandString.Replace( " /?", "" );
            newInput.fullCommandString = newInput.fullCommandString.Replace( " -h", "" );
            newInput.fullCommandString = newInput.fullCommandString.Replace( " /h", "" );
            newInput.fullCommandString = newInput.fullCommandString.Replace( " /help", "" );
            string[] separators = new string[] { " " };
            string[] splitString = newInput.fullCommandString.Split( separators, System.StringSplitOptions.RemoveEmptyEntries );
            newInput.commandName = splitString[ splitString.Length - 1 ];
            newInput.parentCommandsString = newInput.parentCommandsString.Replace( formatStr( newInput.commandName, strFormat.SUB_COMMAND ) + " ", "" );
            /*newInput.parentCommandsString = newInput.parentCommandsString.Replace( formatStr( "-help", strFormat.SUB_COMMAND ) + " ", "" );
            newInput.parentCommandsString = newInput.parentCommandsString.Replace( formatStr( "-?", strFormat.SUB_COMMAND ) + " ", "" );
            newInput.parentCommandsString = newInput.parentCommandsString.Replace( formatStr( "/?", strFormat.SUB_COMMAND ) + " ", "" );
            newInput.parentCommandsString = newInput.parentCommandsString.Replace( formatStr( "-h", strFormat.SUB_COMMAND ) + " ", "" );
            newInput.parentCommandsString = newInput.parentCommandsString.Replace( formatStr( "/h", strFormat.SUB_COMMAND ) + " ", "" );
            newInput.parentCommandsString = newInput.parentCommandsString.Replace( formatStr( "/help", strFormat.SUB_COMMAND ) + " ", "" );*/
            //outputHelpForInput( newInput );
            outputHelpForInput( input );
        }, new string[] { } );
        helpCommand.requiresLogin = false;
        helpCommand.requiresConnection = false;
        helpCommand.requiresTarget = false;
        commands = new List<Command>();

        // Setup
        Command cmd;
        Command subCmd;
        Command subSubCmd;
        Command subSubSubCmd;

        // Echo
        cmd = new Command( "echo", new string[] { "ec" }, "Outputs text to the console.", this, ( CommandInput input ) => {
            if ( input.fullCommandString.Length > input.commandName.Length ) {
                addOutput( input.fullCommandString.Substring( input.commandName.Length + 1 ) );
            } else {
                outputHelpForInput( input );
            }
        }, new string[] { "text to output" } );
        cmd.requiresLogin = false;
        commands.Add( cmd );

        // Help
        cmd = new Command( "help", new string[] { "h", "?" }, "Displays a full list of available commands.", this, ( CommandInput input ) => {
            if ( input.parameters.Count > 0 ) {
                Command c = getBaseCommandFromString( input.parameters[ 0 ] );
                if ( c != null ) {
                    outputHelpForCommand( c );
                    return;
                }
            }
            outputHelpFile();
        }, null );
        cmd.requiresLogin = false;
        commands.Add( cmd );

        // Target
        cmd = new Command( "target", new string[] { "targ", "select", "sel" }, "Targets the object with the specified id.", this, ( CommandInput input ) => {
            if ( input.parameters.Count == 1 ) {
                GameObject[] objects = GameObject.FindGameObjectsWithTag( "Interactable" );
                Interactable interactable = null;
                foreach ( GameObject obj in objects ) {
                    Interactable currentInteractable = obj.GetComponent<Interactable>();
                    if ( currentInteractable != null ) {
                        if ( currentInteractable.displayName.ToLower() == input.parameters[ 0 ] ) {
                            interactable = currentInteractable;
                            break;
                        }
                    }
                }
                if ( interactable != null ) {
                    StartCoroutine( cutsceneSelectTarget( interactable ) );
                } else {
                    addOutput( "Target not found: " + formatStr( input.parameters[ 0 ], strFormat.PARAMETER_IN_INSTRUCTION ) );
                }
            } else {
                outputHelpForInput( input );
            }
        }, new string[] { "target id" } );
        cmd.requiresConnection = true;
        cmd.requiresTarget = false;
        {
            subCmd = new Command( "deselect", new string[] { "des", "-d", "/d", "d" }, "Deselects the current target.", this, ( CommandInput input ) => {
                StartCoroutine( cutsceneDeselectTarget() );
            }, new string[] { } );
            subCmd.requiresTarget = true;
            cmd.subCommands.Add( subCmd );
        }
        commands.Add( cmd );

        // Restart
        cmd = new Command( "restart", new string[] { }, "Restarts the game.", this, ( CommandInput input ) => {
            if ( input.parameters.Count == 1 ) {
                if ( input.parameters[ 0 ] == "-y" ) {
                    Application.LoadLevel( 0 );
                    Destroy( this );
                } else {
                    outputHelpForInput( input );
                }
            } else {
                outputHelpForInput( input );
            }
        }, new string[] { "-y" } );
        cmd.requiresLogin = false;
        commands.Add( cmd );

        // Connect
        cmd = new Command( "connect", new string[] { "conn" }, "Connects to an agent.", this, null, null );
        subCmd = new Command( "agent", new string[] { "a" }, "Connects to the agent with the specified id.", this, ( CommandInput input ) => {
            if ( input.parameters.Count == 1 ) {
                bool found = false;
                int agentId;
                if ( int.TryParse( input.parameters[ 0 ], out agentId ) ) {
                    Mission mission = null;
                    foreach ( Mission m in missions ) {
                        if ( m.agentId == agentId ) {
                            mission = m;
                            if ( m.accepted && !m.completed ) {
                                StartCoroutine( cutsceneConnect( mission, agentId ) );
                                found = true;
                            }
                            break;
                        }
                    }
                    if ( !found ) {
                        if ( mission == null ) {
                            addOutput( "Unable to find agent " + formatStr( "#" + input.parameters[ 0 ], strFormat.PARAMETER_IN_INSTRUCTION ) );
                        } else {
                            if ( !mission.accepted ) {
                                addOutput( "Mission " + formatStr( "#" + mission.id, strFormat.PARAMETER_IN_INSTRUCTION ) + " is not accepted. To connect to this agent you must first " + formatCmd( "accept", new string[] { "mission", "" + mission.id } ) + "." );
                           } else if ( mission.completed ) {
                               addOutput( "Connection request denied. The mission is already completed." );
                           }
                        }
                    }
                } else {
                    addOutput( "" + input.parameters[ 0 ] + " is not a valid agent id." );
                }
            } else {
                outputHelpForInput( input );
            }
        }, new string[] { "agent id" } );
        cmd.subCommands.Add( subCmd );
        commands.Add( cmd );

        // Disconnect
        cmd = new Command( "disconnect", new string[] { "disc" }, "Disconnects from an agent.", this, ( CommandInput input ) => {
            if ( input.parameters.Count == 1 ) {
                if ( input.parameters[ 0 ] == "-y" ) {
                    StartCoroutine( cutsceneDisconnect() );
                } else {
                    outputHelpForInput( input );
                }
            } else {
                outputHelpForInput( input );
            }
        }, new string[] { "-y" } );
        cmd.requiresConnection = true;
        commands.Add( cmd );

        // Log in
        cmd = new Command( "login", new string[] { "logon" }, "Logs you in.", this, ( CommandInput input ) => {
            StartCoroutine( displayWelcomeMessage() );
        }, new string[] { } );
        cmd.requiresLogin = false;
        commands.Add( cmd );

        // Log out
        cmd = new Command( "logout", new string[] { "logoff" }, "Logs you out.", this, ( CommandInput input ) => {
            StartCoroutine( cutsceneLogout() );
        }, new string[] { } );
        cmd.requiresLogin = true;
        commands.Add( cmd );

        // Watch
        cmd = new Command( "watch", new string[] { }, "Gives live feedback from a targeted system.", this, ( CommandInput input ) => {
            addOutput( "Not yet implemented" );
        }, new string[] { "system id" } );
        cmd.requiresConnection = true;
        cmd.requiresTarget = true;
        commands.Add( cmd );

        // Clear
        cmd = new Command( "clear", new string[] { "cls" }, "Clear the screen from text.", this, ( CommandInput input ) => {
            outputHistory = new List<string>();
        }, new string[] { "system id" } );
        cmd.requiresLogin = false;
        commands.Add( cmd );

        // List
        cmd = new Command( "list", new string[] { "ls", "dir" }, "Displays a list of the selected content.", this, null, null );
        // {
            // List MissionList
            subCmd = new Command( "missionlist", new string[] { "missionslist", "missions", "mlist", "ml" }, "Lists all missions.", this, ( CommandInput input ) => {
                bool listNotEmpty = false;

                // Not accepted mission
                bool missionListed = false;
                foreach ( Mission mission in missions ) {
                    if ( !mission.accepted ) {
                        if ( !missionListed ) {
                            addOutput( formatStr( "PENDING MISSIONS", strFormat.HEADLINE ) );
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
                            addOutput( formatStr( "ACTIVE MISSIONS", strFormat.HEADLINE ) );
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
                            addOutput( formatStr( "COMPLETED MISSIONS", strFormat.HEADLINE ) );
                            missionListed = true;
                            listNotEmpty = true;
                        }
                        outputMissionInList( mission );
                    }
                }

                if ( !listNotEmpty ) {
                    addOutput( "Your mission list is empty" );
                }
            }, new string[] { } );
            cmd.subCommands.Add( subCmd );

            // List Mission
            subCmd = new Command( "mission", new string[] { "m" }, "Lists details for the selected mission.", this, ( CommandInput input ) => {
                if ( input.parameters.Count == 1  || ( input.parameters.Count == 0 && currentMission.accepted ) ) {
                    if ( input.parameters.Count == 0 && currentMission.accepted ) {
                        // Do current mission
                        input.parameters.Add( "" + currentMission.id );
                    }

                    int missionId;
                    if ( int.TryParse( input.parameters[ 0 ], out missionId ) ) {
                        bool missionListed = false;
                        foreach ( Mission m in missions ) {
                            if ( m.id == missionId ) {
                                outputMission( m );
                                missionListed = true;
                                break;
                            }
                        }
                        if ( !missionListed ) {
                            addOutput( "Unable to select mission " + formatStr( "" + missionId, strFormat.ID ) + ". Enter " + formatCmd( "list", "missionlist" ) + " to see available missions." );
                        }
                    } else {
                        outputHelpForInput( input );
                    }
                } else {
                    outputHelpForInput( input );
                }
            }, new string[] { "mission id" } );
            cmd.subCommands.Add( subCmd );

            // List Agents
            subCmd = new Command( "agents", new string[] { "a" }, "Lists available agents.", this, ( CommandInput input ) => {
                bool listNotEmpty = false;
                bool didOutput = false;
                foreach ( Mission m in missions ) {
                    if ( m.accepted && !m.completed ) {
                        if ( !didOutput ) {
                            addOutput( formatStr( "AVAILABLE AGENTS (ACTIVE MISSIONS):", strFormat.HEADLINE ) );
                            didOutput = true;
                            listNotEmpty = true;
                        }
                        addOutput( "Agent " + formatStr( "#" + m.agentId, strFormat.ID ) + " for mission " + formatStr( "#" + m.id, strFormat.ID ) + ": " + m.title );
                    }
                }

                didOutput = false;
                foreach ( Mission m in missions ) {
                    if ( !m.accepted ) {
                        if ( !didOutput ) {
                            addOutput( formatStr( "UNAVAILABLE AGENTS (PENDING MISSIONS):", strFormat.HEADLINE ) );
                            didOutput = true;
                            listNotEmpty = true;
                        }
                        addOutput( "Agent " + formatStr( "#" + m.agentId, strFormat.ID ) + " for mission " + formatStr( "#" + m.id, strFormat.ID ) + ": " + m.title );
                        didOutput = true;
                    }
                }

                if ( !listNotEmpty ) {
                    addOutput( "Your agent list is empty" );
                }
            }, new string[] { } );
            cmd.subCommands.Add( subCmd );
        // }
        commands.Add( cmd );

        // Scan
        cmd = new Command( "scan", new string[] { "sc" }, "Scans and outputs the systems of the current target", this, ( CommandInput input ) => {
            StartCoroutine( cutsceneScan() );
        }, new string[] { } );
        cmd.requiresConnection = true;
        cmd.requiresTarget = true;
        commands.Add( cmd );

        // Accept
        cmd = new Command( "accept", new string[] { }, "Accepts a mission.", this, null, null );
        {
            // Mission
            subCmd = new Command( "mission", new string[] { "m" }, "Accepts the selected mission.", this, ( CommandInput input ) => {
                if ( input.parameters.Count == 1 ) {
                    int missionId;
                    if ( int.TryParse( input.parameters[ 0 ], out missionId ) ) {
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
                            addOutput( "Unable to accept mission " + formatStr( input.parameters[ 0 ], strFormat.PARAMETER_IN_INSTRUCTION ) + " - mission id not found. Enter " + formatCmd( "list", "missions" ) + " to see available missions and their ids." );
                        }
                    } else {
                        addOutput( formatStr( input.parameters[ 0 ], strFormat.PARAMETER_IN_INSTRUCTION ) + " - is not a valid mission id. Enter " + formatCmd( "list", "missions" ) + " to see available missions and their ids." );
                    }
                } else {
                    outputHelpForInput( input );
                }
            }, new string[] { "mission id" } );
            cmd.subCommands.Add( subCmd );
        }
        commands.Add( cmd );

        // Enable
        cmd = new Command( "enable", new string[] { "en" }, "Enables the selected system on the current target.", this, ( CommandInput input ) => {
            StartCoroutine( cutsceneEnable( input ) );
        }, new string[] { "system id" } );
        cmd.requiresConnection = true;
        cmd.requiresTarget = true;
        commands.Add( cmd );

        // Disable
        cmd = new Command( "disable", new string[] { "dis" }, "Disables the selected system on the current target.", this, ( CommandInput input ) => {
            StartCoroutine( cutsceneDisable( input ) );
        }, new string[] { "system id" } );
        cmd.requiresConnection = true;
        cmd.requiresTarget = true;
        commands.Add( cmd );

        // Options
        cmd = new Command( "options", new string[] { "opt", "opts" }, "Displays and changes options.", this, null, null );
        cmd.requiresLogin = false;
        {
            // Sound options
            subCmd = new Command( "sound", new string[] { "snd" }, "Displays and changes sound options.", this, null, null );
            subCmd.requiresLogin = false;
            {
                // Linefeed sound options
                subSubCmd = new Command( "linefeed", new string[] { "lf" }, "Displays whether linefeed sound is on or off", this, ( CommandInput input ) => {
                    if ( input.parameters.Count <= 0 ) {
                        addOutput( "Linefeed sound effect is " + ( lineFeedSoundEnabled ? "on" : "off" ) + "." );
                    } else {
                        outputHelpForInput( input );
                    }
                }, new string[] { } );
                subSubCmd.requiresLogin = false;
                {
                    // Linefeed on
                    subSubSubCmd = new Command( "on", new string[] { }, "Enables the linefeed sound effect.", this, ( CommandInput input ) => {
                        lineFeedSoundEnabled = true;
                        addOutput( "Linefeed sound effect enabled." );
                    }, new string[] { } );
                    subSubSubCmd.requiresLogin = false;
                    subSubCmd.subCommands.Add( subSubSubCmd );

                    // Linefeed off
                    subSubSubCmd = new Command( "off", new string[] { }, "Disables the linefeed sound effect.", this, ( CommandInput input ) => {
                        lineFeedSoundEnabled = false;
                        addOutput( "Linefeed sound effect disabled." );
                    }, new string[] { } );
                    subSubSubCmd.requiresLogin = false;
                    subSubCmd.subCommands.Add( subSubSubCmd );
                }
                subCmd.subCommands.Add( subSubCmd );
            }
            cmd.subCommands.Add( subCmd );

            // Timescale options
            subCmd = new Command( "timescale", new string[] { "ts" }, "Displays and changes the time scale multiplier. 1 is normal speed, 0 makes everything instant.", this, ( CommandInput input ) => {
                if ( input.parameters.Count == 1 ) {
                    float ts;
                    if ( float.TryParse( input.parameters[ 0 ], out ts ) ) {
                        timeScale = ts;
                        addOutput( "Time scale is now " + timeScale );
                    } else {
                        addOutput( formatStr( input.parameters[ 0 ], strFormat.PARAMETER_IN_INSTRUCTION ) + " is not a number." );
                    }
                } else if ( input.parameters.Count == 0 ) {
                    addOutput( "Time scale is " + timeScale );
                } else {
                    outputHelpForInput( input );
                }
            }, new string[] { "number" } );
            subCmd.requiresLogin = false;
            cmd.subCommands.Add( subCmd );
        }
        commands.Add( cmd );

        // END OF COMMANDS
    }

    // Connect cutscene
    IEnumerator cutsceneConnect( Mission mission, int agentId )  {
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
    }
    
    // Disconnect cutscene
    IEnumerator cutsceneDisconnect() {
        addOutput( "Closing connection..." );
        yield return new WaitForSeconds( 1.0f * timeScale );
        addOutput( "Connection succesfully closed" );
        connected = false;
        currentMission = new Mission( "", "", 0, 0, "" );
        Application.LoadLevel( "main" );
    }

    // Log out cutscene
    IEnumerator cutsceneLogout() {
        if ( isLoggingIn ) {
            StopCoroutine( displayWelcomeMessage() );
        }
        addOutput( "Logging out..." );
        yield return new WaitForSeconds( 0.5f );
        addOutput( "User successfully logged out" );
        loggedIn = false;
    }

    // Select target cutscene
    IEnumerator cutsceneSelectTarget( Interactable interactable ) {
        addOutput( "Locking target..." );
        yield return new WaitForSeconds( 1.0f * timeScale );
        this.target = interactable;
        addOutput( "Target " + target.displayName + " selected" );
    }

    // Deselect target cutscene
    IEnumerator cutsceneDeselectTarget() {
        if ( target != null ) {
            addOutput( "Deselecting target..." );
            yield return new WaitForSeconds( 0.5f * timeScale );
            target = null;
            addOutput( "No target selected" );
        } else {
            addOutput( "No target selected" );
        }
    }

    // Scan cutscene
    IEnumerator cutsceneScan() {
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

    // Enable cutscene
    IEnumerator cutsceneEnable( CommandInput input ) {
        if ( input.parameters.Count == 1 ) {
            bool found = false;
            foreach ( InteractableSystem isystem in target.systems ) {
                if ( isystem.name.ToLower() == input.parameters[ 0 ] ) {
                    found = true;
                    addOutput( "Enabling system..." );
                    yield return new WaitForSeconds( 0.5f * timeScale );
                    isystem.activate();
                    addOutput( isystem.response );
                }
            }
            if ( !found ) {
                addOutput( "System " + input.parameters[ 0 ] + " not found" );
            }
        } else {
            outputHelpForInput( input );
        }
    }

    // Disable cutscene
    IEnumerator cutsceneDisable( CommandInput input ) {
        if ( input.parameters.Count == 1 ) {
            bool found = false;
            foreach ( InteractableSystem isystem in target.systems ) {
                if ( isystem.name.ToLower() == input.parameters[ 0 ] ) {
                    found = true;
                    addOutput( "Disabling system..." );
                    yield return new WaitForSeconds( 0.5f * timeScale );
                    isystem.deactivate();
                    addOutput( isystem.response );
                }
            }
            if ( !found ) {
                addOutput( "System " + input.parameters[ 0 ] + " not found" );
            }
        } else {
            outputHelpForInput( input );
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
        addOutput( "Enter " + formatCmd( "help" ) + " to load the help file" );
        yield return new WaitForSeconds( 3.0f * timeScale );
        int missionId = generateNewMissionId();
        int agentId = generateNewAgentId();
        Mission mission = new Mission( "Seize launch codes", "Agent " + formatStr( "#" + agentId, strFormat.ID ) + " is in need of assistance. You need to connect to the agent and retrieve the launch codes. Connect to agent " + formatStr( "#" + agentId, strFormat.ID ) + " by entering " + formatCmd( "connect", new string[] { "agent", "" + agentId } ) + " and take control of the situation.", missionId, agentId, "level1" );
        mission.addObjective( new MissionObjective( "Connect to the agent", "Connect to the agent by entering " + formatCmd( "connect", new string[] { "agent", "" + agentId } ), 1, () => {
            if ( Application.loadedLevelName == mission.scene ) {
                return true;
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
            commandHistory = new List<CommandInput>();
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
                    CommandInput command = new CommandInput( currentInput );
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
        if ( currentMarkerPosition <= buffer.Length ) {
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

    void execute( CommandInput command ) {
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
    void runCommand( CommandInput input ) {
        foreach ( Command cmd in commands ) {
            if ( cmd.matchesInputCommandWithCommandInput( input ) ) {
                cmd.process( input );
                return;
            }
        }
        outputInvalidCommand( input );

        /*if ( loggedIn ) {
            switch ( input.commandName ) {
                case "target": {
                        StartCoroutine( cmdTarget( input ) );
                        break;
                    }
                case "scan": {
                        StartCoroutine( cmdScan( input ) );
                        break;
                    }
                case "accept": {
                        cmdAccept( input );
                        break;
                    }
                case "en":
                case "enable": {
                        StartCoroutine( cmdEnable( input ) );
                        break;
                    }
                case "dis":
                case "disable": {
                        StartCoroutine( cmdDisable( input ) );
                        break;
                    }
                case "conn":
                case "connect": {
                        StartCoroutine( cmdConnect( input ) );
                        break;
                    }
                case "disc":
                case "disconnect": {
                        StartCoroutine( cmdDisconnect( input ) );
                        break;
                    }
                case "options":
                case "opt": {
                        cmdOptions( input );
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
                            execute( new CommandInput( "disconnect -y" ) );
                        }
                        StartCoroutine( cmdLogout( input ) );
                        break;
                    }
                case "list":
                case "dir":
                case "ls": {
                        cmdList( input );
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
                        outputHelpFile();
                        break;
                    }
                case "restart":
                case "r": {
                        cmdRestart( input );
                        break;
                    }
                case "clear":
                case "cls": {
                        if ( input.parameters.Count == 0 ) {
                            outputHistory = new List<string>();
                        } else {
                            outputInvalidFormat( input, "clear" );
                        }
                        break;
                    }
                default: {
                        outputInvalidCommand( input );
                        break;
                    }
            }
        } else {
            switch ( input.commandName ) {
                case "login":
                case "logon": {
                        cmdLogin( input );
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
                        cmdOptions( input );
                        break;
                    }
                default: {
                        addOutput( "You are not logged in. Log in using command: login" );
                        break;
                    }
            }
        }*/
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
    // Formatting
    public enum strFormat { BASE_COMMAND, ALIAS, SUB_COMMAND, CURRENT_SUB_COMMAND, DESCRIPTION, PARAMETER_NAME, PARAMETER_IN_INSTRUCTION, HEADLINE, ID };
    public string formatStr( string str, strFormat format ) {
        switch ( format ) {
            case strFormat.BASE_COMMAND:
            case strFormat.HEADLINE:
                return "<b>" + str + "</b>";
            case strFormat.SUB_COMMAND:
            case strFormat.ALIAS:
            case strFormat.ID:
            case strFormat.PARAMETER_IN_INSTRUCTION:
                return "<i>" + str + "</i>";
            case strFormat.PARAMETER_NAME:
                return "<" + str + ">";
            default:
                return str;
        }
    }
    // Command formatting
    public string formatCmd( string baseCommand ) {
        return formatStr( baseCommand, strFormat.BASE_COMMAND );
    }
    public string formatCmd( string baseCommand, string[] subCommands ) {
        string ret = formatCmd( baseCommand );
        foreach ( string subCmd in subCommands ) {
            ret += " ";
            ret += formatStr( subCmd, strFormat.SUB_COMMAND );
        }
        return ret;
    }
    public string formatCmd( string baseCommand, string[] subCommands, string[] parameters ) {
        string ret = formatCmd( baseCommand, subCommands );
        foreach ( string parameter in parameters ) {
            ret += " ";
            ret += formatStr( formatStr( parameter, strFormat.PARAMETER_NAME ), strFormat.PARAMETER_IN_INSTRUCTION );
        }
        return ret;
    }
    // Command formatting aliases
    public string formatCmd( string baseCommand, string subCommand ) {
        return formatCmd( baseCommand, new string[] { subCommand } );
    }
    public string formatCmd( string baseCommand, string subCommand, string parameter ) {
        return formatCmd( baseCommand, new string[] { subCommand }, new string[] { parameter } );
    }
    public string formatCmd( string baseCommand, string subCommand, string[] parameters ) {
        return formatCmd( baseCommand, new string[] { subCommand }, parameters );
    }
    public string formatCmd( string baseCommand, string[] subCommands, string parameter ) {
        return formatCmd( baseCommand, subCommands, new string[] { parameter } );
    }

    // Output
    public void addOutput( string output ) {
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


    public void addOutputWithSpacing( string output ) {
        addOutput( "" );
        addOutput( output );
    }


    public void addOutputIfNotEmpty( string str ) {
        string testStr = str.Replace( " ", "" );
        if ( testStr != "" ) {
            addOutput( str );
        }
    }


    // String constructing functions
    public string getCommandAliases( Command cmd ) {
        string aliases = "";
        foreach ( string alias in cmd.aliases ) {
            if ( aliases != "" ) {
                aliases += ", ";
            } else {
                aliases += "Aliases: " + formatStr( cmd.name, strFormat.ALIAS ) + ", ";
            }
            aliases +=  formatStr( alias, strFormat.ALIAS );
        }
        return aliases;
    }

    public string getSubCommandAliases( CommandInput input ) {
        Command cmd = getCurrentCommandFromInput( input );
        string aliases = "";
        foreach ( string alias in cmd.aliases ) {
            if ( aliases != "" ) {
                aliases += ", ";
            } else {
                aliases += "Aliases: " + formatStr( cmd.name, strFormat.ALIAS ) + ", ";
            }
            aliases +=  formatStr( alias, strFormat.ALIAS );
        }
        return aliases;
    }


    public string getCommandParameters( Command cmd ) {
        string coreFunctionParameters = "";
        if ( cmd.coreFunction != null ) {
            foreach ( string param in cmd.coreFunctionParameterNames ) {
                coreFunctionParameters += " " + formatStr( param, strFormat.PARAMETER_NAME );
            }
            coreFunctionParameters = formatStr( cmd.name, strFormat.BASE_COMMAND ) + coreFunctionParameters + " - " + formatStr( cmd.description, strFormat.DESCRIPTION );
        }
        return coreFunctionParameters;
    }

    public string getSubCommandParameters( CommandInput input ) {
        Command cmd = getCurrentCommandFromInput( input );
        string coreFunctionParameters = "";
        if ( cmd.coreFunction != null ) {
            foreach ( string param in cmd.coreFunctionParameterNames ) {
                coreFunctionParameters += " " + formatStr( param, strFormat.PARAMETER_NAME );
            }
            coreFunctionParameters = input.parentCommandsString + formatStr( cmd.name, strFormat.CURRENT_SUB_COMMAND ) + coreFunctionParameters + " - " + formatStr( cmd.description, strFormat.DESCRIPTION );
        }
        return coreFunctionParameters;
    }


    public string getCommandSubCommands( Command cmd ) {
        string subCommands = "";
        bool first = true;
        foreach ( Command subCommand in cmd.subCommands ) {
            if ( !first ) {
                subCommands += "\n";
            }
            subCommands += formatStr( cmd.name, strFormat.BASE_COMMAND ) + " " + formatStr( subCommand.name, strFormat.CURRENT_SUB_COMMAND ) + " - " + formatStr( subCommand.description, strFormat.DESCRIPTION );
            first = false;
        }
        return subCommands;
    }

    public string getSubCommandSubCommands( CommandInput input ) {
        Command cmd = getCurrentCommandFromInput( input );
        string subCommands = "";
        bool first = true;
        foreach ( Command subCommand in cmd.subCommands ) {
            if ( !first ) {
                subCommands += "\n";
            }
            subCommands += input.parentCommandsString + formatStr( cmd.name, strFormat.CURRENT_SUB_COMMAND ) + " " + formatStr( subCommand.name, strFormat.CURRENT_SUB_COMMAND ) + " - " + formatStr( subCommand.description, strFormat.DESCRIPTION );
            first = false;
        }
        return subCommands;
    }

    // Command tree for base command
    public string getCommandTreeForBaseCommand( Command cmd ) {
        CommandInput input = new CommandInput( cmd.name );
        string cmdTree = "";
        if ( cmd.coreFunction != null ) {
            string coreFunctionParameters = "";
            foreach ( string param in cmd.coreFunctionParameterNames ) {
                coreFunctionParameters += " " + formatStr( param, strFormat.PARAMETER_NAME );
            }
            cmdTree += formatStr( cmd.name, strFormat.BASE_COMMAND ) + coreFunctionParameters + " - " + formatStr( cmd.description, strFormat.DESCRIPTION );
        }
        foreach ( Command subCmd in cmd.subCommands ) {
            CommandInput newInput = new CommandInput( input.fullCommandString + " " + subCmd.name );
            newInput = subCmd.reformatCommandInputForSubCommand( newInput );
            cmdTree += getRecursiveSubCommandTreeFromBaseCommand( subCmd, newInput );
        }
        if ( cmdTree.StartsWith( "\n" ) ) {
            cmdTree = cmdTree.Substring( 1 );
        }
        return cmdTree;
    }
    // Command tree generation recursive function
    private string getRecursiveSubCommandTreeFromBaseCommand( Command cmd, CommandInput input ) {
        string cmdTree = "";
        if ( cmd.coreFunction != null ) {
            string coreFunctionParameters = "";
            foreach ( string param in cmd.coreFunctionParameterNames ) {
                coreFunctionParameters += " " + formatStr( param, strFormat.PARAMETER_NAME );
            }
            cmdTree += "\n" + input.parentCommandsString + formatStr( cmd.name, strFormat.CURRENT_SUB_COMMAND ) + coreFunctionParameters + " - " + formatStr( cmd.description, strFormat.DESCRIPTION );
        }
        foreach ( Command subCmd in cmd.subCommands ) {
            CommandInput newInput = new CommandInput( input.fullCommandString + " " + subCmd.name );
            newInput.parentCommandsString = input.parentCommandsString;
            newInput.commandName = cmd.name;
            newInput = subCmd.reformatCommandInputForSubCommand( newInput );
            cmdTree += getRecursiveSubCommandTreeFromBaseCommand( subCmd, newInput );
        }
        return cmdTree;
    }

    // Command tree for sub command
    /*public string getCommandTreeForSubCommand( CommandInput input ) {
        Command cmd = getCurrentCommandFromInput( input );
        string cmdTree = "";
        if ( cmd.coreFunction != null ) {
            string coreFunctionParameters = "";
            foreach ( string param in cmd.coreFunctionParameterNames ) {
                coreFunctionParameters += " " + formatStr( param, strFormat.PARAMETER_NAME );
            }
            cmdTree += formatStr( cmd.name, strFormat.BASE_COMMAND ) + coreFunctionParameters + " - " + formatStr( cmd.description, strFormat.DESCRIPTION );
        }
        input.parameters.RemoveRange( 0, input.parameters.Count );
        foreach ( Command subCmd in cmd.subCommands ) {
            CommandInput newInput = new CommandInput( input.fullCommandString + " " + subCmd.name );
            newInput = subCmd.reformatCommandInputForSubCommand( newInput );
            cmdTree += getRecursiveSubCommandTreeFromSubCommand( subCmd, newInput );
        }
        if ( cmdTree.StartsWith( "\n" ) ) {
            cmdTree = cmdTree.Substring( 1 );
        }
        return cmdTree;
    }
    // Command tree generation recursive function
    private string getRecursiveSubCommandTreeFromSubCommand( Command cmd, CommandInput input ) {
        string cmdTree = "";
        if ( cmd.coreFunction != null ) {
            string coreFunctionParameters = "";
            foreach ( string param in cmd.coreFunctionParameterNames ) {
                coreFunctionParameters += " " + formatStr( param, strFormat.PARAMETER_NAME );
            }
            cmdTree += "\n" + input.parentCommandsString + formatStr( cmd.name, strFormat.CURRENT_SUB_COMMAND ) + coreFunctionParameters + " - " + formatStr( cmd.description, strFormat.DESCRIPTION );
        }
        foreach ( Command subCmd in cmd.subCommands ) {
            CommandInput newInput = new CommandInput( input.fullCommandString + " " + subCmd.name );
            newInput.parentCommandsString = input.parentCommandsString;
            newInput.commandName = subCmd.name;
            newInput = subCmd.reformatCommandInputForSubCommand( newInput );
            cmdTree += getRecursiveSubCommandTreeFromBaseCommand( subCmd, newInput );
        }
        return cmdTree;
    }*/


    // Output functions
    public void outputInternalError( string message ) {
        addOutput( "INTERNAL ERROR: " + message );
    }
    /*public void outputInvalidFormat( CommandInput cmd, string expectedFormat ) {
        addOutput( "Command " + formatStr( cmd.commandName, strFormat.BASE_COMMAND ) + " invalid format. Expected:\n" + expectedFormat );
    }*/
    public void outputInvalidCommand( CommandInput input ) {
        addOutput( "Unknown command " + formatCmd( input.commandName ) );
        addOutput( "Enter " + formatCmd( "help" ) + " for a list of available commands" );
    }
    public void outputRequiresLogin( Command cmd ) {
        addOutput( "Command " + formatCmd( cmd.name ) + " requires the user to be logged in." );
        addOutput( "Enter " + formatCmd( "login" ) + " to log in." );
    }
    public void outputRequiresConnection( Command cmd ) {
        addOutput( "Command " + formatCmd( cmd.name ) + " requires an active connection to an agent." );
        addOutput( "Use the " + formatCmd( "connect" ) + " command to connect to an agent. Enter " + formatCmd( "connect", "-help" ) + " for more information." );
    }
    public void outputRequiresTarget( Command cmd ) {
        addOutput( "Command " + formatCmd( cmd.name ) + " requires the user to be logged in." );
        addOutput( "Use the " + formatCmd( "target" ) + " command to target an object. Enter " + formatCmd( "target", "-help" ) + " for more information." );
    }
    /*public void outputExpectedFormat( Command cmd, CommandInput input ) {
        addOutput( "Unexpected format: " + input.fullCommandString );
        addOutput( "Possible formats:" );
        if ( cmd.coreFunction != null ) {
            string coreFunctionParameters = "";
            foreach ( string param in cmd.coreFunctionParameterNames ) {
                coreFunctionParameters += formatStr( param, strFormat.PARAMETER_NAME );
            }
            addOutput( input.parentCommandsString + formatStr( cmd.name, strFormat.CURRENT_SUB_COMMAND ) + " - " + formatStr( cmd.description, strFormat.DESCRIPTION ) );
        }
        foreach ( Command subCommand in cmd.subCommands ) {
            addOutput( input.parentCommandsString + formatStr( cmd.name, strFormat.CURRENT_SUB_COMMAND ) + " " + formatStr( subCommand.name, strFormat.BASE_COMMAND ) + " - " + formatStr( subCommand.description, strFormat.DESCRIPTION ) );
        }
    }*/
    public void outputHelpForInput( CommandInput input ) {
        if ( input.baseCommandName == input.commandName ) { // Base command
            outputHelpForCommand( getBaseCommandFromInput( input ) );
        } else {
            outputHelpForSubCommand( input );
        }
    }
    public void outputHelpForCommand( Command cmd ) {
        addOutput( "=== HELP FILE FOR " + formatCmd( cmd.name ) );
        addOutputIfNotEmpty( getCommandAliases( cmd ) );
        addOutputIfNotEmpty( cmd.description );
        addOutputIfNotEmpty( getCommandTreeForBaseCommand( cmd ) );
        //addOutputIfNotEmpty( getCommandParameters( cmd ) );
        //addOutputIfNotEmpty( getCommandSubCommands( cmd ) );
    }
    public void outputHelpForSubCommand( CommandInput input ) {
        //Command cmd = getCurrentCommandFromInput( input );
        //Command baseCmd = getBaseCommandFromInput( input );
        Command cmd = getBaseCommandFromInput( input );
        addOutput( "=== HELP FILE FOR " + formatCmd( cmd.name ) );
        addOutputIfNotEmpty( getCommandAliases( cmd ) );
        addOutputIfNotEmpty( cmd.description );
        addOutputIfNotEmpty( getCommandTreeForBaseCommand( cmd ) );
        //addOutput( "=== HELP FILE FOR " + input.parentCommandsString + formatStr( cmd.name, strFormat.CURRENT_SUB_COMMAND ) );
        //addOutputIfNotEmpty( cmd.description );
        //addOutputIfNotEmpty( getSubCommandAliases( input ) );
        //string[] separators = new string[] { "\n" };
        //string[] lines = getCommandTreeForBaseCommand( baseCmd ).Split( separators, System.StringSplitOptions.None );
        //string output = "";
        //foreach ( string line in lines ) {
            //if ( line.Contains( input.parentCommandsString ) ) {
                //output += line + "\n";
            //}
        //}
        //addOutputIfNotEmpty( output );
        //addOutputIfNotEmpty( getCommandTreeForBaseCommand( baseCmd ) );
        //addOutputIfNotEmpty( getSubCommandParameters( input ) );
        //addOutputIfNotEmpty( getSubCommandSubCommands( input ) );
    }
    /*public void outputHelpOld( CommandInput cmd, string help ) {
        addOutput( "Help file for command " + formatStr( cmd.commandName, strFormat.BASE_COMMAND ) + "\n" + help );
    }
    public void outputCommandNotYetImplemented( CommandInput cmd ) {
        addOutput( "This command has not yet been implemented." );
    }*/
    public void outputHelpFile( ) {
        addOutput( "=== HELP FILE ===" );
        addOutput( formatStr( "General instructions:", strFormat.HEADLINE ) );
        addOutput( "You control everything using your console." );
        addOutput( "If you want more information about a specific command, or a shorthand for its parameters, you can enter " + formatCmd( "<command>", "-help" ) );
        addOutput( formatStr( "List of available commands:", strFormat.HEADLINE ) );
        foreach ( Command cmd in commands ) {
            addOutput( formatCmd( cmd.name ) + " - " + formatStr( cmd.description, strFormat.DESCRIPTION ) );
        }
        /*addOutput( "Starting and ending" );
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
        addOutput( " restart (r) - restart the game" );*/
        addOutput( "" );
        addOutput( "To view currently available missions, enter " + formatCmd( "list", "missions" ) );
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
        addOutputWithSpacing( "*** New mission received - " + formatStr( "#" + mission.id, strFormat.ID ) + ": " + mission.title + "\nEnter " + formatCmd( "list", new string[] { "mission", "" + mission.id } ) + " for more info" );
    }
    public IEnumerator addMissionObjective( Mission mission, MissionObjective objective ) {
        if ( canAddMissionObjectives ) {
            foreach ( Mission m in missions ) {
                if ( m.id == mission.id ) {
                    m.addObjective( objective );
                    addOutputWithSpacing( "*** New objective received: " + objective.title + "\nEnter " + formatCmd( "list", new string[] { "mission", "" + m.id } ) + " for more details." );
                    break;
                }
            }
        } else {
            yield return new WaitForSeconds( 1.0f );
            StartCoroutine( addMissionObjective( mission, objective ) );
        }
    }
    void outputMissionComplete( Mission m ) {
        addOutputWithSpacing( "Mission completed. " + formatStr( "#" + m.id, strFormat.ID ) + ": " + m.title
                            + "\nEnter disconnect -y to give control back to the agent and return to the lobby." );
    }
    void outputObjectiveComplete( Mission m, MissionObjective o ) {
        addOutputWithSpacing( "Objective " + o.title + " completed. Enter " + formatCmd( "list", new string[] { "mission", "" + m.id } ) + " for a list of objectives" );
        o.runCompletionFunction();
    }
    void outputMissionInList( Mission m ) {
        addOutput( "Mission " + formatStr( "#" + m.id, strFormat.ID ) + ": " + m.title + ( m.accepted ? " - Agent " + formatStr( "#" + m.agentId, strFormat.ID ) : " - Enter " + formatCmd( "accept", new string[] { "mission", "" + m.id } ) + " to accept or " + formatCmd( "list", new string[] { "mission", "" + m.id } ) + " for more information." ) );
            //+ "\nAccepted: [" + ( m.accepted ? "X" : " " ) + "]" 
            //+ " Completed: [" + ( m.completed ? "X" : " " ) + "]" );
    }
    void outputMission( Mission m ) {
        addOutput( "========================================" );
        string status;
        if ( m.completed ) {
            status = "COMPLETED";
        } else if ( m.accepted ) {
            status = "ACTIVE";
        } else {
            status = "PENDING ACCEPTANCE";
        }
        addOutput( formatStr( "MISSION " + formatStr( "#" + m.id, strFormat.ID ) + " - " + m.title, strFormat.HEADLINE )
            + ( m.accepted ? "" : "\nEnter " + formatCmd( "accept", new string[] { "mission", "" + m.id } ) + " to accept." ) 
            + "\nSTATUS: " + status
            + "\nAGENT: " + formatStr( "#" + m.agentId, strFormat.ID )
            + "\n\n" + formatStr( "MISSION BRIEFING", strFormat.HEADLINE )
            //+ "\nAccepted: [" + ( m.accepted ? "X" : " " ) + "]"
            //+ " Completed: [" + ( m.completed ? "X" : " " ) + "]"
            + "\n" + m.description + "\n\n" + formatStr( "OBJECTIVES", strFormat.HEADLINE ) );

        bool first = true;
        foreach ( MissionObjective o in m.objectives ) {
            if ( !first ) {
                addOutput( "" );
            }
            first = false;
            addOutput( formatStr( o.title, strFormat.HEADLINE ) + " - " + ( o.completed ? "" : "NOT " ) + "COMPLETED"
                + "\n" + o.description );
        }
    }
    void testMissionCompletion() {
        canAddMissionObjectives = false;
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
        canAddMissionObjectives = true;
        Invoke( "testMissionCompletion", missionCompletionTestInterval );
    }

    ////////////////////////////////////////
    //
    // COMMANDS
    //
    ////////////////////////////////////////
    Command getBaseCommandFromString( string cmdName ) {
        foreach ( Command cmd in commands ) {
            if ( cmd.matchesInputCommandWithString( cmdName ) ) {
                return cmd;
            }
        }
        return null;
    }
    Command getBaseCommandFromInput( CommandInput input ) {
        input.commandName = input.baseCommandName;
        foreach ( Command cmd in commands ) {
            if ( cmd.matchesInputCommandWithCommandInput( input ) ) {
                return cmd;
            }
        }
        return null;
    }
    Command getCommandFromString( string cmdName, List<Command> cmdList ) {
        foreach ( Command cmd in cmdList ) {
            if ( cmd.matchesInputCommandWithString( cmdName ) ) {
                return cmd;
            }
        }
        return null;
    }
    Command getCurrentCommandFromInput( CommandInput input ) {
        List<Command> cmdList = commands;
        CommandInput currentInput = new CommandInput( input.fullCommandString );
        Command cmd = getBaseCommandFromInput( currentInput );
        while ( cmd != null && cmd.name != input.commandName && currentInput.parameters.Count > 0 ) {
            cmd = getCommandFromString( currentInput.parameters[ 0 ], cmd.subCommands );
            if ( cmd != null ) {
                currentInput.commandName = cmd.name;
                currentInput.parameters.RemoveAt( 0 );
            }
        }
        return cmd;
    }


}
