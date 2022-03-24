using UnityEngine;
using UnityEngine.UI;
public class ContentSizeFitterRefresh : MonoBehaviour
{
    private void Start()
    {
        Invoke("RefreshContentFitters", 0.1f);
    }

    public void RefreshContentFitters()
    {
        RefreshContentFitter((RectTransform)transform);
    }
    private void RefreshContentFitter(RectTransform transform)
    {
        if (transform == null || !transform.gameObject.activeSelf)
        {
            return;
        }
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform childTransform = transform.GetChild(i).GetComponent<RectTransform>();    
            if (childTransform != null) RefreshContentFitter(childTransform);
        }

        var layoutGroup = transform.GetComponent<LayoutGroup>();
        var contentSizeFitter = transform.GetComponent<ContentSizeFitter>();
        if (layoutGroup != null)
        {
            layoutGroup.SetLayoutHorizontal();
            layoutGroup.SetLayoutVertical();
        }

        if (contentSizeFitter != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform);
        }
    }
}
