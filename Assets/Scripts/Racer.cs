using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Racer : MonoBehaviour,IResettable
{
    
    public bool activeSelf { get { return gameObject.activeSelf; } set { } }
    public void SetActive(bool value)
    {
        gameObject.SetActive(value);
    }

}