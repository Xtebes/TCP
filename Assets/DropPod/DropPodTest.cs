using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropPodTest : MonoBehaviour
{
    public Animator anim;
    public bool open;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(open)
        {
            anim.SetBool("Open", true);
        }
        else
        {
            anim.SetBool("Open", false);
        }
    }
}
