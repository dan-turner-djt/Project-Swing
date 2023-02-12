using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericAnimationData
{
    public string name;
    public string boolName;
    public string triggerName;

    public GenericAnimationData (string _name, string _boolName, string _triggerName)
    {
        name = _name;
        boolName = _boolName;
        triggerName = _triggerName;
    }
}
