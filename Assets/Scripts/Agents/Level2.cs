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
                        if ( isystem.identifier == 1 ) {
                            if ( !isystem.enabled ) {
                                StartCoroutine( secondDialog() );
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
        yield return new WaitForSeconds( 10.0f * mainConsole.timeScale );
        addOutput( "In fact, it's quite relaxing up here..." );
    }

    public IEnumerator secondDialog() {
        StopCoroutine( firstDialog() );
        addOutput( "THAT'S NOT QUITE WHAT I HAD IN MIND!" );
        yield return new WaitForSeconds( 4.0f * mainConsole.timeScale );
        addOutput( "Now I know who NOT to call next time." );
        yield return new WaitForSeconds( 4.0f * mainConsole.timeScale );
        addOutput( "You know what? Just leave me here. I'll be alright!" );
        yield return new WaitForSeconds( 5.0f * mainConsole.timeScale );
        while ( true ) {
            addOutput( "Just enter " + mainConsole.formatCmd( "disconnect", "-y" ) + " and get out of here, okay?" );
            yield return new WaitForSeconds( 5.0f * mainConsole.timeScale );
            addOutput( "Yes, this was a short mission. Just get out!" );
            yield return new WaitForSeconds( 4.0f * mainConsole.timeScale );
            addOutput( "WHY ARE YOU STILL HERE!?" );
            yield return new WaitForSeconds( 3.0f * mainConsole.timeScale );
            addOutput( "Please, this is extremely embarassing." );
            yield return new WaitForSeconds( 8.0f * mainConsole.timeScale );
        }
    }
}
