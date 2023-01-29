using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public enum STUDY_STATE
{
    MAIN_MENU,
    BRIEF,
    MEMORIZATION,
    WAITING,
    KEYPAD_INPUT,
    FOLLOW_UP,
    DEBRIEF
}

public class StudyController : MonoBehaviour
{
    private UIManager _ui;

    private STUDY_STATE _state;

    private void Awake()
    {
        _state = STUDY_STATE.MAIN_MENU;
        _ui = GetComponent<UIManager>();
    }

    public void BeginNewTrial()
    {
        _state = STUDY_STATE.BRIEF;
        Debug.Log("Beginning new trial.");
        Debug.Log("Briefing participant.");
        _ui.ChangePanel(_state);
    }

    public void CompleteBrief()
    {
        Debug.Log("Participant briefed.");
        StartMemorization();
    }

    private void StartMemorization()
    {
        Debug.Log("Starting memorization.");
        _state = STUDY_STATE.MEMORIZATION;
        _ui.ChangePanel(_state);
    }

    private void StartWaiting(float waitInSeconds)
    {
        Debug.Log("Waiting for " + waitInSeconds.ToString() + "seconds.");
        _state = STUDY_STATE.WAITING;
        _ui.ChangePanel(_state);
    }

    private void StartKeypad()
    {
        Debug.Log("Starting keypad.");
        _state = STUDY_STATE.KEYPAD_INPUT;
        _ui.ChangePanel(_state);
    }
    
    public void LogInput(int integer)
    {
        Debug.Log(integer.ToString() + " logged as input.");
    }

    public void QuitSoftware()
    {
        Application.Quit();
    }
    
    
}
