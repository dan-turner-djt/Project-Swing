using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextBoxTesting : GeneralSceneController
{
    GeneralSceneController sc;

    string testText = "test";

    private void Awake()
    {
        DoAwake();
    }

    private void Start()
    {
        DoStart();

        TextBoxManager tbm = GetComponent<TextBoxManager>();
        bool loaded = tbm.LoadTextSequenceInformation(testText);
    }

    void Update()
    {
        DoUpdate();


        if (Input.GetKeyDown (KeyCode.Q))
        {
            StartTextSequence(testText, true);
        }
    }
}
