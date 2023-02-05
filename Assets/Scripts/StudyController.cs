using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using Unity.VisualScripting;


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
    private const string CSV_OUTPUT_PATH = "Assets/CSV Output";

    private const char DELIMITER = ',';

    private readonly string[] columnNames = { "ID", "Keypad", "RT", "Accuracy" };

    private UIManager _ui;
    private AudioManager _audio;

    private STUDY_STATE _state;

    private List<String> _shuffledCodes;
    private List<String> _shuffledKeypads;

    private int _currentTrial;
    private int _maxTrials;

    private string _logged;
    private float _reactionTimeStart;
    private string _trialStart;
    private int _accurateCharacters;

    private string _rawData;

    public KeypadTrial[] keypadTrials;

    private void Awake()
    {
        
        _state = STUDY_STATE.MAIN_MENU;
        _ui = GetComponent<UIManager>();
        _audio = GetComponent<AudioManager>();
        _ui.CloseAllPanels();
        _ui.ChangePanel(STUDY_STATE.MAIN_MENU);
    }
    private void InitializeTrial()
    {
        _trialStart = DateTime.Now.ToString();
        
        // Read random codes and shuffle them
        string[] unshuffledCodes = File.ReadAllText(RANDOM_CODES_PATH).Split(',');
        _shuffledCodes = unshuffledCodes.OrderBy(a => Guid.NewGuid()).ToList(); // Shuffle codes by generating new Global Unique Identifiers (GUIDs)
        
        _currentTrial = 0;
        _maxTrials = 0;
        _shuffledKeypads = new List<string>();
        foreach (KeypadTrial trial in keypadTrials)
        {
            _maxTrials += trial.trialCount;
            for(int i=0; i<trial.trialCount; i++)
                _shuffledKeypads.Add(trial.id);   
        }

        _shuffledKeypads = _shuffledKeypads.OrderBy(a => Guid.NewGuid()).ToList();

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
        if (_state == STUDY_STATE.MEMORIZATION)
        {
            Debug.LogError("Tried to start memorization while already in memorization.");
            return;
        }
        
        Debug.Log("Starting memorization.");
        _state = STUDY_STATE.MEMORIZATION;
        _ui.ChangePanel(_state);
        StartCoroutine(PlayMemorizationAudio(_shuffledCodes[_currentTrial], 0.5f, 1.5f, 2));
    }

    private void StartWaiting(float waitInSeconds, STUDY_STATE nextState)
    {
        Debug.Log("Waiting for " + waitInSeconds.ToString() + " seconds.");
        _state = STUDY_STATE.WAITING;
        _ui.ChangePanel(_state);
        StartCoroutine(IdleWaiting(waitInSeconds, nextState));
    }

    private void StartKeypad()
    {
        _logged = "";
        Debug.Log("Starting " + _shuffledKeypads[_currentTrial] + " keypad.");
        _state = STUDY_STATE.KEYPAD_INPUT;
        _ui.ChangePanel(_state, _shuffledKeypads[_currentTrial]);
        _ui.ClearDigitText();

        _reactionTimeStart = Time.time;
    }
    
    public void LogInput(int integer)
    {
        Debug.Log("Current trial -> " + _shuffledCodes[_currentTrial].Length.ToString());
        if (_logged.Length < _shuffledCodes[_currentTrial].Length)
        {
            _logged += integer.ToString();
            _ui.LogDigitProgress(_logged.Length);
            Debug.Log(integer.ToString() + " logged as input.");   
        }
        
        // Finish trial
        if (_logged.Length >= _shuffledCodes[_currentTrial].Length)
        {
            float RT = Time.time - _reactionTimeStart;
            _accurateCharacters = 0;
            for(int i = 0; i<_logged.Length; i++)
                if (_logged[i]==_shuffledCodes[_currentTrial][i])
                    ++_accurateCharacters;
            
            Debug.Log(_shuffledCodes[_currentTrial] + " -> " + _logged);
            Debug.Log(_shuffledKeypads[_currentTrial] + " -> RT: " + RT.ToString() + " | Accuracy: " + _accurateCharacters.ToString() + "/" + _logged.Length);
            
            LogResult(_trialStart,  _shuffledKeypads[_currentTrial], RT, _accurateCharacters);

            ++_currentTrial;
            if (_currentTrial < _shuffledKeypads.Count)
                Invoke("StartMemorization", 0.5f);
            else
            {
                SaveData();
                EnterState(STUDY_STATE.DEBRIEF);
            }
        }
    }

    IEnumerator PlayMemorizationAudio(String digits, float delayInBetweenDigits=0.5f, float delayInBetweenRepeats=1.5f, int repeats = 2)
    {
        for (int iteration = 0; iteration < repeats; iteration++)
        {
            for (int i = 0; i < digits.Length; i++)
                yield return new WaitForSeconds(_audio.PlayDigit((char)digits[i])+delayInBetweenDigits);
            yield return new WaitForSeconds(delayInBetweenRepeats);
        }
        
        Debug.Log("Memorization is complete. Proceeding.");
        StartWaiting(5f, STUDY_STATE.KEYPAD_INPUT);
    }

    IEnumerator IdleWaiting(float waitTimeInSeconds, STUDY_STATE nextState)
    {
        float waitEnd = Time.time + waitTimeInSeconds;
        while (waitEnd < Time.time)
        {
            _ui.UpdateWaitingSlider((waitEnd-Time.time)/waitTimeInSeconds);
            yield return new WaitForEndOfFrame();
        }
        
        EnterState(nextState);
    }

    // Log a new row of data into the final CSV
    void LogResult(string id, string keyboard, float RT, int accuracy)
    {
        _rawData += id + DELIMITER + keyboard + DELIMITER + RT.ToString() + DELIMITER + accuracy + '\n';
    }
    
    private void SaveData()
    {
        DirectoryInfo destination = new DirectoryInfo(CSV_OUTPUT_PATH);
        if(!destination.Exists)
            destination.Create();

        StreamWriter outputFile = File.CreateText(destination + "test" + ".csv");
        
        string finalOutput = "";
        for (int i = 0; i < columnNames.Length; i++)
            finalOutput += columnNames[i] + (i<(columnNames.Length-1)? DELIMITER : '\n');

        finalOutput += _rawData;
        
        outputFile.Write(finalOutput);
        outputFile.Close();
    }

    private void EnterState(STUDY_STATE newState)
    {
        switch (newState)
        {
            case STUDY_STATE.MAIN_MENU:
                break;
            case STUDY_STATE.BRIEF:
                break;
            case STUDY_STATE.MEMORIZATION:
                StartMemorization();
                break;
            case STUDY_STATE.WAITING:
                break;
            case STUDY_STATE.KEYPAD_INPUT:
                StartKeypad();
                break;
            case STUDY_STATE.FOLLOW_UP:
                break;
            case STUDY_STATE.DEBRIEF:
                _ui.ChangePanel(STUDY_STATE.DEBRIEF);
                break;
            default:
                break;
        }
    }

    public void ReturnToMainMenu()
    {
        _ui.ChangePanel(STUDY_STATE.MAIN_MENU);
    }
    public void QuitSoftware()
    {
        Application.Quit();
    }
    
    
}
