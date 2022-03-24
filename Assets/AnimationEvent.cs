using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvent : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void Play(string path)
    {
        FMODUnity.RuntimeManager.PlayOneShotAttached(path, gameObject);
    }
}
