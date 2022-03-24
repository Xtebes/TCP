using UnityEngine;
using UnityEngine.InputSystem;
public class Input : MonoBehaviour
{
    public System.Action loadedInput;  
    public InputActionAsset inputActionAsset;
    [HideInInspector]
    public InputAction mousePosition
                       ,bodyMovement, jump
                       ,fire ,switchMode
                       ,toggleHUD, toggleUpgraders;
    void Start()
    {
        mousePosition = inputActionAsset.FindAction("mousePosition");
        bodyMovement = inputActionAsset.FindAction("BodyMovement");
        jump = inputActionAsset.FindAction("Jump");
        fire = inputActionAsset.FindAction("Fire");
        switchMode = inputActionAsset.FindAction("SwitchMode");
        toggleHUD = inputActionAsset.FindAction("ToggleHUD");
        toggleUpgraders = inputActionAsset.FindAction("ToggleUpgraders");
        loadedInput.Invoke();        
    }
}
