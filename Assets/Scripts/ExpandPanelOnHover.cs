using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class ExpandPanelOnHover : MonoBehaviour
{
    public enum State { Icon, Tooltip, Expanded }
    private State currentState = State.Icon;

    [Header("Background & Layout")]
    public RectTransform bgRect;
    public RectTransform iconRef;     
    public RectTransform tooltipRef;  
    public RectTransform expandedRef; 

    [Header("Content Roots (The Objects to Enable/Disable)")]
    public GameObject iconRoot;
    public GameObject tooltipRoot;
    public GameObject expandedRoot;

    [Header("Content Faders (The CanvasGroups)")]
    public CanvasGroup iconGroup;
    public CanvasGroup tooltipGroup;
    public CanvasGroup expandedGroup;

    [Header("Settings")]
    public float transitionTime = 0.2f;

    private bool isHovering;
    private bool isSelected;
    private Coroutine currentAnim;

    public void OnHoverEnter() => SetHover(true);
    public void OnHoverExit() => SetHover(false);
    
    public void OnSelect()
    {
        isSelected = !isSelected;
        UpdateState();
    }

    void SetHover(bool state)
    {
        isHovering = state;
        UpdateState();
    }

    void UpdateState()
    {
        State target = State.Icon;
        
        if (isSelected) target = State.Expanded;
        else if (isHovering) target = State.Tooltip;

        if (target != currentState)
        {
            currentState = target;
            StartTransition(currentState);
        }
    }

    void StartTransition(State target)
    {
        // Enable the ROOTS so the hierarchy is visible
        if(iconRoot) iconRoot.SetActive(true);
        if(tooltipRoot) tooltipRoot.SetActive(true);
        if(expandedRoot) expandedRoot.SetActive(true);

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(AnimateToState(target));
    }

    IEnumerator AnimateToState(State target)
    {
        // Setup targets
        RectTransform finalTransform = target == State.Icon ? iconRef : 
                                       target == State.Tooltip ? tooltipRef : expandedRef;

        float targetIconA = target == State.Icon ? 1f : 0f;
        float targetToolA = target == State.Tooltip ? 1f : 0f;
        float targetExpA  = target == State.Expanded ? 1f : 0f;

        // Cache start values
        Vector3 startPos = bgRect.position;
        Quaternion startRot = bgRect.rotation;
        Vector2 startSize = bgRect.sizeDelta;

        float startIconA = iconGroup.alpha;
        float startToolA = tooltipGroup.alpha;
        float startExpA = expandedGroup.alpha;

        float t = 0f;
        while (t < transitionTime)
        {
            t += Time.deltaTime;
            float p = t / transitionTime;

            // Lerp Rect
            bgRect.position = Vector3.Lerp(startPos, finalTransform.position, p);
            bgRect.rotation = Quaternion.Lerp(startRot, finalTransform.rotation, p);
            bgRect.sizeDelta = Vector2.Lerp(startSize, finalTransform.sizeDelta, p);

            // Crossfade Alphas
            iconGroup.alpha = Mathf.Lerp(startIconA, targetIconA, p);
            tooltipGroup.alpha = Mathf.Lerp(startToolA, targetToolA, p);
            expandedGroup.alpha = Mathf.Lerp(startExpA, targetExpA, p);

            yield return null;
        }

        // Snap to finish
        bgRect.position = finalTransform.position;
        bgRect.sizeDelta = finalTransform.sizeDelta;
        
        iconGroup.alpha = targetIconA;
        tooltipGroup.alpha = targetToolA;
        expandedGroup.alpha = targetExpA;

        // Disable unused ROOTS to save performance
        if(iconRoot) iconRoot.SetActive(target == State.Icon);
        if(tooltipRoot) tooltipRoot.SetActive(target == State.Tooltip);
        if(expandedRoot) expandedRoot.SetActive(target == State.Expanded);
    }
}