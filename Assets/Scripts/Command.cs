using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CommandInput {
    public string fullCommandString;
    public string commandName;
    public List<string> parameters;
    public string parentCommandsString = "";
    public string baseCommandName;

    public CommandInput( string commandString ) {
        this.fullCommandString = commandString;
        this.parameters = new List<string>();
        string[] separators = new string[] { " " };
        string[] splitString = commandString.Split( separators, System.StringSplitOptions.RemoveEmptyEntries );
        this.commandName = splitString[ 0 ].ToLower();
        this.baseCommandName = this.commandName;
        for ( int i = 1; i < splitString.Length; i++ ) {
            splitString[ i ] = splitString[ i ].Replace( "#", "" );
            parameters.Add( splitString[ i ].ToLower() );
        }
    }
}

public class Command {
    public string name;
    public string[] aliases;
    public string description;
    public delegate void CoreFunction( CommandInput input );
    public List<Command> subCommands;
    public CoreFunction coreFunction;
    public string[] coreFunctionParameterNames;
    public bool requiresLogin = true;
    public bool requiresConnection = false;
    public bool requiresTarget = false;
    private ConsoleGUI parent;
    public ConsoleGUI.baseCommandCategories commandCategory;

    public Command( string name, string[] aliases, string description, ConsoleGUI parent, CoreFunction functionOrNull, string[] coreFunctionParameterNames, ConsoleGUI.baseCommandCategories commandCategory ) {
        this.name = name;
        if ( aliases == null ) {
            aliases = new string[] { };
        }
        this.aliases = aliases;
        this.description = description;
        this.coreFunction = functionOrNull;
        this.subCommands = new List<Command>();
        this.parent = parent;
        this.coreFunctionParameterNames = coreFunctionParameterNames;
        this.commandCategory = commandCategory;
    }

    public void process( CommandInput input ) {
        // Display help even if the command can't be run in the current state
        if ( input.parameters.Count > 0 ) {
            if ( parent.helpCommand.matchesInputParameter( input ) ) {
                executeSubCommand( input, parent.helpCommand );
                return;
            }
        }
        // Check if the command can be run in the current state
        if ( requiresLogin && !parent.loggedIn ) {
            parent.outputRequiresLogin( this );
            return;
        }
        if ( requiresConnection && !parent.connected ) {
            parent.outputRequiresConnection( this );
            return;
        }
        if ( requiresTarget && parent.target == null ) {
            parent.outputRequiresTarget( this );
            return;
        }
        // Try to run a sub command
        if ( input.parameters.Count > 0 ) {
            foreach ( Command cmd in subCommands ) {
                if ( cmd.matchesInputParameter( input ) ) {
                    executeSubCommand( input, cmd );
                    return;
                }
            }
        }
        // If no sub command was run, run the core command or display the help file
        if ( coreFunction != null ) {
            coreFunction( input );
        } else {
            outputHelp( input );
        }
    }

    public void executeSubCommand( CommandInput input, Command subCommand ) {
        CommandInput newInput = reformatCommandInputForSubCommand( input );
        subCommand.process( newInput );
    }

    public CommandInput reformatCommandInputForSubCommand( CommandInput input ) {
        if ( input.parameters.Count > 0 ) {
            CommandInput newInput = input;
            if ( input.parentCommandsString == "" ) {
                newInput.parentCommandsString += parent.formatStr( input.commandName, ConsoleGUI.strFormat.BASE_COMMAND ) + " ";
            } else {
                newInput.parentCommandsString += parent.formatStr( input.commandName, ConsoleGUI.strFormat.SUB_COMMAND ) + " ";
            }
            newInput.commandName = newInput.parameters[ 0 ];
            newInput.parameters.RemoveAt( 0 );
            return newInput;
        }
        return null;
    }

    public void outputHelp( CommandInput input ) {
        parent.outputHelpForInput( input );
    }

    public bool matchesInputParameter( CommandInput input ) {
        //parent.addOutput( "Matches Input Command " + parent.formatStr( input.parameters[ 0 ] , ConsoleGUI.strFormat.BASE_COMMAND ) + " in command " + parent.formatStr( name, ConsoleGUI.strFormat.BASE_COMMAND ) );
        bool matches = false;
        if ( input.parameters.Count > 0 ) {
            //parent.addOutput( "testing " + name );
            if ( input.parameters[ 0 ] == name ) {
                matches = true;
                //parent.addOutput( "subcommand matches exactly" );
            }
            if ( !matches ) {
                foreach ( string alias in aliases ) {
                    // parent.addOutput( "testing " + alias );
                    if ( input.parameters[ 0 ] == alias ) {
                        matches = true;
                        //parent.addOutput( "subcommand matches alias" );
                        break;
                    }
                }
            }
            //if ( !matches ) {
                //parent.addOutput( "no matching subcommand" );
            //}
        } else {
            parent.outputInternalError( "Missing parameters" );
            return false;
        }
        return matches;
    }
    public bool matchesInputCommandWithCommandInput( CommandInput input ) {
        //parent.addOutput( "Matches Input Command " + parent.formatStr( input.commandName, ConsoleGUI.strFormat.BASE_COMMAND ) + " in command " + parent.formatStr( name, ConsoleGUI.strFormat.BASE_COMMAND ) );
        bool matches = false;
        //parent.addOutput( "testing " + name );
        if ( input.commandName == name ) {
            matches = true;
            //parent.addOutput( "subcommand matches exactly" );
        }
        if ( !matches ) {
            foreach ( string alias in aliases ) {
                // parent.addOutput( "testing " + alias );
                if ( input.commandName == alias ) {
                    matches = true;
                    //parent.addOutput( "subcommand matches alias" );
                    break;
                }
            }
        }
        //if ( !matches ) {
            //parent.addOutput( "no matching subcommand" );
        //}
        return matches;
    }
    public bool matchesInputCommandWithString( string input ) {
        //parent.addOutput( "Matches Input Command " + parent.formatStr( input.commandName, ConsoleGUI.strFormat.BASE_COMMAND ) + " in command " + parent.formatStr( name, ConsoleGUI.strFormat.BASE_COMMAND ) );
        bool matches = false;
        //parent.addOutput( "testing " + name );
        if ( input == name ) {
            matches = true;
            //parent.addOutput( "subcommand matches exactly" );
        }
        if ( !matches ) {
            foreach ( string alias in aliases ) {
                // parent.addOutput( "testing " + alias );
                if ( input == alias ) {
                    matches = true;
                    //parent.addOutput( "subcommand matches alias" );
                    break;
                }
            }
        }
        //if ( !matches ) {
            //parent.addOutput( "no matching subcommand" );
        //}
        return matches;
    }
}
