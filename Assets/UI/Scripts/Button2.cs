using UnityEngine.EventSystems;
using UnityEngine.UI;
public class Button2 : Button
{
    public override void OnSubmit(BaseEventData eventData)
    {
        base.OnSubmit(eventData);
        onClick.Invoke();
    }
    public override void Select()
    {
        base.Select();
        onClick.Invoke();
    }
}
