using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimation : MonoBehaviour
{
    public Animator anim;
    private InputAction fire, switchmode, moving, jump;

    private void LoadInput(Input input)
    {
        fire = input.fire;
        switchmode = input.switchMode;
        moving = input.bodyMovement;
        jump = input.jump;
    }

    private void Update()
    {
        if (fire.triggered)
        {
            anim.SetTrigger("Shoot");
        }
        if (switchmode.triggered)
        {
            anim.SetBool("EnergyMode", !anim.GetBool("EnergyMode"));
            anim.SetTrigger("Switch");
        }
    }
}