using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponTest : MonoBehaviour
{
    public Animator anim;
    public bool cryo;
    public bool isShooting;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(cryo)
        {
            anim.SetBool("SwitchCryo", true);
        }
        else
        {
            anim.SetBool("SwitchCryo", false);
        }

        if(isShooting)
        {
            anim.SetBool("IsShootingEnergy", true);
        }
        else
        {
            anim.SetBool("IsShootingEnergy", false);
        }
    }
}
