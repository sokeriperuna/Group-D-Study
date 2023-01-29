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
    }

    public void CompleteBrief()
    {
        Debug.Log("Participant briefed.");
    }

    private void StartMemorization()
    {
        Debug.Log("Starting memorization.");
        _state = STUDY_STATE.MEMORIZATION;
    }

    private void StartWaiting(float waitInSeconds)
    {
        Debug.Log("Waiting for " + waitInSeconds.ToString() + "seconds.");
        _state = STUDY_STATE.WAITING;
    }

    private void StartKeypad()
    {
        _state = STUDY_STATE.KEYPAD_INPUT;
        Debug.Log("Starting keypad.");
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
