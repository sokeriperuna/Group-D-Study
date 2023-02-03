using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public AudioClip[] baseTenDigits;

    private AudioSource _audioSource;

    private const int ZERO_CHAR = 48;
    private const int NINE_CHAR = 57;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }
    
    // Plays digit, returns length of clip in seconds
    public float PlayDigit(int n){
        if (n < 0 | n > baseTenDigits.Length)
        {
            Debug.LogError("Invalid digit: " + (char)(n+ZERO_CHAR) + " <-> " + (n+ZERO_CHAR).ToString());
            return 0;
        }

        AudioClip digitAudioClip = baseTenDigits[n];
        _audioSource.clip = digitAudioClip;
        _audioSource.Play();

        return digitAudioClip.length;
    }

    public float PlayDigit(Char digit) { return PlayDigit((int)digit-ZERO_CHAR); }
}
