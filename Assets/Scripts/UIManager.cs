using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Windows.Speech;
using TMPro;

[System.Serializable]
public struct KeypadPanel
{
    public string id;
    public RectTransform panel;
}

public class UIManager : MonoBehaviour
{
    public RectTransform launchPage;
    public RectTransform studyBrief;
    public RectTransform studyDebrief;

    public RectTransform memorization;
    public RectTransform waiting;

    public RectTransform keypadPanel;
    public TMP_Text digitProgress;
    public KeypadPanel[] keypads;

    private Dictionary<String, RectTransform> _keypads;
    private string _activeKeypad; 

    private void Awake()
    {
        
        
        // Init. private dictionary
        //foreach (var k in keypads)
        //    _keypads.Add(k.id, k.panel);
    }

    public void LogDigitProgress(int progress)
    {
        string output = "";
        for (int i = 0; i < progress; i++)
            output += i>0 ? '*' : " *";
        
        digitProgress.text = output;
    }

    public void ChangePanel(STUDY_STATE newPanel)
    {
        CloseAllPanels();
        switch (newPanel)
        { 
            case STUDY_STATE.MAIN_MENU:
                launchPage.gameObject.SetActive(true);
                break;
            case STUDY_STATE.BRIEF:
                studyBrief.gameObject.SetActive(true);
            break;
            case STUDY_STATE.MEMORIZATION:
                memorization.gameObject.SetActive(true);
                break;
            case STUDY_STATE.WAITING:
                waiting.gameObject.SetActive(true);
                break;
            case STUDY_STATE.KEYPAD_INPUT:
                keypadPanel.gameObject.SetActive(true);
                break;
            case STUDY_STATE.FOLLOW_UP:
                break;
            case STUDY_STATE.DEBRIEF:
                studyDebrief.gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }

    public void CloseAllPanels()
    { 
        launchPage.gameObject.SetActive(false);
        studyBrief.gameObject.SetActive(false);
        memorization.gameObject.SetActive(false);
        waiting.gameObject.SetActive(false);
        keypadPanel.gameObject.SetActive(false);
        studyDebrief.gameObject.SetActive(false);
        
        foreach (var k in keypads)
            k.panel.gameObject.SetActive(false);
    }
}
