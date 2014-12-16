using UnityEngine;
using System.Collections;

public class Level2 : Agent {

	// Use this for initialization
	void Start () {
        initializeAgent();
        init = () => {
            StartCoroutine( firstDialog() );
        };
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public IEnumerator firstDialog() {
        yield return new WaitForSeconds( 1.0f * mainConsole.timeScale );
        addOutput( "Hey!" );
        yield return new WaitForSeconds( 2.0f * mainConsole.timeScale );
        addOutput( "First of all, I just want to make it clear that this mission isn't fully created yet." );
        yield return new WaitForSeconds( 4.0f * mainConsole.timeScale );
        addOutput( "I'm in a bit of trouble." );
        yield return new WaitForSeconds( 3.0f * mainConsole.timeScale );
        addOutput( "I need you to get me down." );
        yield return new WaitForSeconds( 2.0f * mainConsole.timeScale );
        addOutput( "In one piece would be nice." );
        yield return new WaitForSeconds( 3.0f * mainConsole.timeScale );
        addOutput( "It looks there's something you can " + mainConsole.formatCmd( "target" ) + " over there!" );
        yield return new WaitForSeconds( 2.0f * mainConsole.timeScale );
        // Add objective
        MissionObjective objective = new MissionObjective( "Get the agent down", "Find a way to get the agent down from the giant red thing.", 2, () => {
            GameObject[] interactables = GameObject.FindGameObjectsWithTag( "Interactable" );
            foreach ( GameObject other in interactables ) {
                Interactable interactable = other.GetComponent<Interactable>();
                if ( interactable.id == 0 ) {
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
        yield return new WaitForSeconds( 3.0f * mainConsole.timeScale );
        addOutput( "And you just take your time, a little blood to the head never killed anyone." );
        yield return new WaitForSeconds( 5.0f * mainConsole.timeScale );
        addOutput( "In fact, it's quite relaxing up here..." );
    }
}
