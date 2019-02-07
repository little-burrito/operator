using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Mission {

    public string title;
    public string description;
    public List<MissionObjective> objectives;
    public bool accepted;
    public bool completed;
    public int id;
    public int agentId;
    public string scene;

    public Mission( string title, string description, int id, int agentId, string scene ) {
        this.title = title;
        this.description = description;
        this.id = id;
        this.agentId = agentId;
        this.scene = scene;
        objectives = new List<MissionObjective>();
        accepted = false;
        completed = false;
    }

    public void addObjective( MissionObjective objective ) {
        objectives.Add( objective );
    }

    public void acceptMission() {
        accepted = true;
    }

    public bool completeMission() {
        if ( objectives.Count == 0 ) {
            return false;
        }
        bool allCompleted = true;
        foreach ( MissionObjective objective in objectives ) {
            if ( !objective.completed ) {
                allCompleted = false;
            }
        }
        if ( allCompleted ) {
            completed = true;
        }
        return completed;
    }
}

public class MissionObjective {

    public string title;
    public string description;
    public int id;
    public bool completed;
    public delegate bool objectiveCompletedTest();
    public delegate void runOnCompletion();
    private objectiveCompletedTest completionConditions; 
    private runOnCompletion completionFunction; 

    public MissionObjective( string title, string description, int id, objectiveCompletedTest customCompletionConditions, runOnCompletion customCompletionFunction ) {
        this.title = title;
        this.description = description;
        this.id = id;
        this.completionConditions = customCompletionConditions;
        this.completionFunction = customCompletionFunction;
        completed = false;
    }

    public bool completeObjective() {
        completed = completionConditions();
        return completed;
    }

    public void runCompletionFunction() {
        completionFunction();
    }
}
