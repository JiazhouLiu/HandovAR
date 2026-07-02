using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class UIPanelController : MonoBehaviour
{
    public XRSlider bedSlider;
    
    // Index 0: Neuro, 1: Resp, 2: CVS, 3: GI, 4: Renal, 5: MSK
    public GameObject[] panels; 

    void Start()
    {
        if (bedSlider)
            bedSlider.onValueChange.AddListener(UpdateActivePanel);
    }

    public void UpdateActivePanel(float val)
    {
        if (panels.Length == 0) return;

        // Map 0-1 slider to array index
        int targetIndex = Mathf.RoundToInt(val * (panels.Length - 1));
        
        for(int i = 0; i < panels.Length; i++)
        {
            if(panels[i]) panels[i].SetActive(i == targetIndex);
        }
    }
}