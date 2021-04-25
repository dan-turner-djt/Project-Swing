using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintObjectController : MonoBehaviour
{
    public string textSequenceName;


    private void Start()
    {
        GeneralSceneController sc = GameObject.FindGameObjectWithTag("SceneController").GetComponent<GeneralSceneController>();
        TextBoxManager tbm = sc.GetComponent<TextBoxManager>();

        bool loaded = tbm.LoadTextSequenceInformation(textSequenceName);
    }
}
