using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;

public class DiggingScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;

    public KMSelectable[] buttons;
    public Material[] colors;
    public GameObject cube;
    public GameObject rotator;

    private readonly int[] outerEdges = { 31, 32, 33, 36, 37, 38, 41, 42, 43, 56, 57, 58, 61, 62, 63, 66, 67, 68, 81, 82, 83, 86, 87, 88, 91, 92, 93 };
    private readonly int[] primes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113 };
    private int[] colorIndexes = new int[125];
    private int breakCount = 0;
    private bool animating = false;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
    }

    void Start () {
        int correct = outerEdges[UnityEngine.Random.Range(0, outerEdges.Length)];
        for (int i = 0; i < 125; i++)
        {
            if (i == correct)
            {
                buttons[i].gameObject.GetComponent<Renderer>().material = colors[13];
                colorIndexes[i] = 13;
            }
            else
            {
                int choice = UnityEngine.Random.Range(0, 13);
                buttons[i].gameObject.GetComponent<Renderer>().material = colors[choice];
                colorIndexes[i] = choice;
            }
        }
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && !animating)
        {
            if (Array.IndexOf(buttons, pressed) >= 125)
            {
                pressed.AddInteractionPunch();
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                int[] xVals = new int[] { 1, 0, 0, -1 };
                int[] zVals = new int[] { 0, 1, -1, 0 };
                StartCoroutine(Rotate(xVals[Array.IndexOf(buttons, pressed) - 125], zVals[Array.IndexOf(buttons, pressed) - 125]));
            }
            else
            {
                if (IsValidBreak(colorIndexes[Array.IndexOf(buttons, pressed)], Array.IndexOf(buttons, pressed)))
                {
                    pressed.gameObject.SetActive(false);
                    breakCount++;
                    if (colorIndexes[Array.IndexOf(buttons, pressed)] == 13)
                    {
                        moduleSolved = true;
                        GetComponent<KMBombModule>().HandlePass();
                    }
                }
                else
                    GetComponent<KMBombModule>().HandleStrike();
            }
        }
    }

    private bool IsValidBreak(int color, int broken)
    {
        if (color == 0 && (bomb.GetModuleNames().Count % 2) == 1 && ((int)bomb.GetTime() % 10) == (bomb.GetModuleNames().Count % 10))
            return true;
        if (color == 1 && !bomb.IsIndicatorPresent(Indicator.IND) && !bomb.IsIndicatorPresent(Indicator.FRK) && ((int)bomb.GetTime() % 10) != 3 && ((int)bomb.GetTime() % 10) != 5)
            return true;
        //List<int> adjCubes = new List<int>();
        //List<int> offsets = new List<int>() { -6, -5, -4, -1, 1, 4, 5, 6 };
        //if (color == 3 && adjCubes.Count != 0 && ((int)bomb.GetTime() % 10) == (breakCount % 10))
        //return true;
        if (color == 4 && ((int)bomb.GetTime() % 60) == (bomb.GetModuleNames().Count % 60))
            return true;
        if (color == 5 && !outerEdges.Contains(broken) && ((int)bomb.GetTime() % 2 == 0 && (((int)bomb.GetTime() % 60) / 10) % 2 == 0) || ((int)bomb.GetTime() % 2 == 1 && (((int)bomb.GetTime() % 60) / 10) % 2 == 1))
            return true;
        if (color == 6 && bomb.GetTwoFactorCodes().Where(x => x % 10 == (int)bomb.GetTime() % 10).Count() > 0)
            return true;
        if (color == 7 && (bomb.GetPortCount() == 0 && ((int)bomb.GetTime() % 10) == 4) || (bomb.GetPortCount() == 1 && ((int)bomb.GetTime() % 10) == 8))
            return true;
        if (color == 8 && primes.Contains(breakCount) && !primes.Contains((int)bomb.GetTime() % 60))
            return true;
        if (color == 9 && (bomb.GetModuleIDs().Contains("ubermodule") || bomb.GetModuleIDs().Contains("bigegg") || bomb.GetModuleIDs().Contains("Laundry")) && ((int)bomb.GetTime() % 10).EqualsAny(1, 2, 3, 5, 8))
            return true;
        if (color == 10 && (bomb.GetIndicators().Count() == 0 || (bomb.GetOffIndicators().Count() > 0 && bomb.GetOnIndicators().Count() > 0)) && (((int)bomb.GetTime() % 10) == (bomb.GetIndicators().Count() % 10)))
            return true;
        //if (color == 11)
            //return true;
        if (color == 12 && bomb.GetBatteryCount() >= 3 && ((int)bomb.GetTime() % 10).EqualsAny(1, 4, 9))
            return true;
        if (color == 13)
            return true;
        return false;
    }

    private IEnumerator Rotate(int xVal, int zVal)
    {
        animating = true;
        for (int i = 0; i < 30; i++)
        {
            cube.transform.RotateAround(rotator.transform.position, rotator.transform.right, 3f * xVal);
            cube.transform.RotateAround(rotator.transform.position, rotator.transform.forward, 3f * zVal);
            yield return new WaitForSeconds(0.005f);
        }
        animating = false;
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} something [Does something]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*something\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            Debug.Log("Did something");
            yield break;
        }
    }
}