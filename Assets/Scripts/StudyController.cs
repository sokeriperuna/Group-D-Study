using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;


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

[System.Serializable]
public struct KeypadTrial
{
    public string id;
    public int trialCount;
}

[RequireComponent(typeof(UIManager))]
[RequireComponent(typeof(AudioManager))]
public class StudyController : MonoBehaviour
{
    private const string RANDOM_CODES_PATH = "Assets/Resources/randomCodes.csv";

    private UIManager _ui;
    private AudioManager _audio;

    private STUDY_STATE _state;

    private List<String> shuffledCodes;
    private List<String> shuffledKeypads;

    private int _currentTrial;
    private int _maxTrials;
    

    public KeypadTrial[] keypadTrials;
    

    private void Awake()
    {
        _state = STUDY_STATE.MAIN_MENU;
        _ui = GetComponent<UIManager>();
        _audio = GetComponent<AudioManager>();
    }
    private void InitializeTrial()
    {
        // Read random codes and shuffle them
        string[] unshuffledCodes = File.ReadAllText(RANDOM_CODES_PATH).Split(',');
        shuffledCodes = unshuffledCodes.OrderBy(a => Guid.NewGuid()).ToList(); // Shuffle codes by generating new Global Unique Identifiers (GUIDs)
        
        _currentTrial = 0;
        _maxTrials = 0;
        shuffledKeypads = new List<string>();
        foreach (KeypadTrial trial in keypadTrials)
        {
            _maxTrials += trial.trialCount;
            for(int i=0; i<trial.trialCount; i++)
                shuffledKeypads.Add(trial.id);   
        }

        shuffledKeypads = shuffledKeypads.OrderBy(a => Guid.NewGuid()).ToList();


        /*foreach (var k in shuffledKeypads)
            Debug.Log(k);

        foreach (string code in shuffledCodes)
            Debug.Log(code + " : " + code.Length);*/

        Debug.Log("Trial initialized.");
    } 

    public void BeginNewTrial()
    {
        InitializeTrial();
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
        StartCoroutine(PlayMemorizationAudio(shuffledCodes[_currentTrial], 0.5f, 2));
    }

    private void StartWaiting(float waitInSeconds)
    {
        Debug.Log("Waiting for " + waitInSeconds.ToString() + " seconds.");
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

    IEnumerator PlayMemorizationAudio(String digits, float delayInBetween=0.5f, int repeats = 2)
    {
        for (int iteration = 0; iteration < repeats; iteration++)
            for (int i = 0; i < digits.Length; i++)
                yield return new WaitForSeconds(_audio.PlayDigit((char)digits[i])+delayInBetween);
        
        Debug.Log("Memorization is complete. Proceeding.");
        StartWaiting(5f);
    }

    IEnumerator IdleWaiting(float waitTimeInSeconds)
    {
        yield return waitTimeInSeconds;
        StartKeypad();
    }

    public void QuitSoftware()
    {
        Application.Quit();
    }
    
    
}
