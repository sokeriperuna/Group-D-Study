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

    public RectTransform keypadPanel;
    public TMP_Text digitProgress;
    public KeypadPanel[] keypads;

    private Dictionary<String, RectTransform> _keypads;
    private string _activeKeypad; 

    private void Awake()
    {
        // Init. private dictionary
        foreach (var k in keypads)
            _keypads.Add(k.id, k.panel);
    }

    public void LogDigitProgress(int progress)
    {
        string output = "";
        for (int i = 0; i < progress; i++)
            output += i>0 ? '*' : " *";
        
        digitProgress.text = output;
    }
}
