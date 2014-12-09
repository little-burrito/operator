using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Command {
    public string fullCommandString;
    public string command;
    public List<string> parameters;

    public Command( string commandString ) {
        this.fullCommandString = commandString;
        parameters = new List<string>();
        string[] separators = new string[] { " " };
        string[] splitString = commandString.Split( separators, System.StringSplitOptions.RemoveEmptyEntries );
        this.command = splitString[ 0 ].ToLower();
        for ( int i = 1; i < splitString.Length; i++ ) {
            splitString[ i ] = splitString[ i ].Replace( "#", "" );
            parameters.Add( splitString[ i ].ToLower() );
        }
    }
}
