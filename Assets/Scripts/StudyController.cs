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
    INTERMISSION,
    FOLLOW_UP,
    DEBRIEF,
    TEST_TRIAL
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
    private const string CSV_OUTPUT_PATH = "Assets/CSVOutput";

    private const char
        DELIMITER = ';'; // NOTE: Excel's default CSV delimiter is a semicolon (ie. ";"). The delimiter can easily be changed later

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

    private string _participantAge;

    public KeypadTrial[] keypadTrials;

    private bool testTrialInProgress = false;

    private void Awake()
    {
        _state = STUDY_STATE.MAIN_MENU;
        _ui = GetComponent<UIManager>();
        _audio = GetComponent<AudioManager>();
        _ui.CloseAllPanels();
        _ui.ChangePanel(STUDY_STATE.MAIN_MENU);
    }

    // bad hacked together emergency measure. deadlines are approaching
    private readonly string[] unshuffledCodes = { "1593","9235","9753","8459","9274","2185","8041","1401","7485","8356","7281","9750","1264","4306","9138","7915","4963","7203","6254","1893","8427","1689" };
    private void InitializeTrial()
    {
        _trialStart = DateTime.Now.ToString();
        
        // Read random codes and shuffle them
        //string[] unshuffledCodes = File.ReadAllText(RANDOM_CODES_PATH).Split(',');
        
        // stuff is breaking...
        //#if UNITY_STANDALONE
        //unshuffledCodes = Resources.Load<TextAsset>(RANDOM_CODES_PATH).text.Split(',');
        //#endif
        
        


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
        _state = STUDY_STATE.KEYPAD_INPUT;
        if (!testTrialInProgress)
        {
            Debug.Log("Starting " + _shuffledKeypads[_currentTrial] + " keypad.");
            _ui.ChangePanel(_state, _shuffledKeypads[_currentTrial]);
        }
        else
            _ui.ChangePanel(_state, "test");
        _ui.ClearDigitText();

        _reactionTimeStart = Time.time;
    }
    
    public void LogInput(int integer)
    {
        if (testTrialInProgress)
        {
            _logged += integer.ToString();
            _ui.LogDigitProgress(_logged.Length);
            if (_logged.Length >= 2)
            {
                ClearData();
                testTrialInProgress = false;
                EnterState(STUDY_STATE.MAIN_MENU);
            }
            return;
        }

        if (_state == STUDY_STATE.FOLLOW_UP)
        {
            _participantAge += integer.ToString();
            _ui.UpdateFollowUpDigits(_participantAge);
            return;
        }
        
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
                EnterState(STUDY_STATE.INTERMISSION);
            else
            {
                _state = STUDY_STATE.FOLLOW_UP;
                EnterState(STUDY_STATE.FOLLOW_UP);
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
        while (Time.time < waitEnd)
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

    public void BeginTestTrial()
    {
        _state = STUDY_STATE.TEST_TRIAL;
        testTrialInProgress = true;
        EnterState(_state);
    }
    
    private void SaveData()
    {
        DirectoryInfo destination = new DirectoryInfo(CSV_OUTPUT_PATH);

        #if  UNITY_STANDALONE
        destination = new  DirectoryInfo(Application.dataPath + "/CSVOutput");
        #endif
        
        if(!destination.Exists)
            destination.Create();


        StreamWriter outputFile = File.CreateText(destination + "/" + _trialStart.Replace('/','_').Replace(':',';') + ".csv"); // the program doesn't like certain characters so I'm swapping them out for other characters
        
        string finalOutput = "";
        for (int i = 0; i < columnNames.Length; i++)
            finalOutput += columnNames[i] + DELIMITER;
        finalOutput += "Age\n";

        string modifiedRawData = "";
        int index = 0;
        while (index < _rawData.Length)
        {
            if (_rawData[index] != '\n')
                modifiedRawData += _rawData[index];
            else
                modifiedRawData += DELIMITER + _participantAge + '\n';
            index++;
        }

        finalOutput += modifiedRawData;
        
        outputFile.Write(finalOutput);
        outputFile.Close();
    }

    private void ClearData()
    {
        _rawData = "";
    }

    private void EnterState(STUDY_STATE newState)
    {
        switch (newState)
        {
            case STUDY_STATE.MAIN_MENU:
                _ui.ChangePanel(STUDY_STATE.MAIN_MENU);
                break;
            case STUDY_STATE.BRIEF:
                _ui.ChangePanel(STUDY_STATE.BRIEF);
                break;
            case STUDY_STATE.MEMORIZATION:
                StartMemorization();
                break;
            case STUDY_STATE.WAITING:
                break;
            case STUDY_STATE.KEYPAD_INPUT:
                StartKeypad();
                break;
            case STUDY_STATE.INTERMISSION:
                _ui.ChangePanel(STUDY_STATE.INTERMISSION);
                break;
            case STUDY_STATE.FOLLOW_UP:
                _ui.ChangePanel(STUDY_STATE.FOLLOW_UP);
                break;
            case STUDY_STATE.DEBRIEF:
                _ui.ChangePanel(STUDY_STATE.DEBRIEF);
                break;
            case STUDY_STATE.TEST_TRIAL:
                _ui.ChangePanel(STUDY_STATE.MEMORIZATION);
                StartCoroutine(PlayMemorizationAudio("12"));
                break;
            default:
                break;
        }
    }

    public void ClearParticipantAge()
    {
        _participantAge = "";
        _ui.UpdateFollowUpDigits("");
    }

    public void ConfirmParticipantAge()
    {
        SaveData();
        EnterState(STUDY_STATE.DEBRIEF);
    }

    public void IntermissionContinue()
    {
        EnterState(STUDY_STATE.MEMORIZATION);
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
