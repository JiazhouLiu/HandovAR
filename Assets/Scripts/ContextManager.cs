using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ContextManager : MonoBehaviour
{
    public TextMeshProUGUI panelTitleText;
    public CanvasGroup panelAlpha;

    // Keyword map: Word -> (Category, Priority)
    // Priority: 0 = Direct match, 1 = Strong keyword, 2 = Weak keyword
    private Dictionary<string, (string title, int priority)> keywordMap = new Dictionary<string, (string, int)>();
    private string currentCategory = "";

    void Awake()
    {
        RegisterKeywords("Respiratory", 0, "respiratory");
        RegisterKeywords("Respiratory", 1, "airway", "lungs", "vent", "ventilator", "sats", "breathing");

        RegisterKeywords("Cardiovascular", 0, "cardiovascular");
        RegisterKeywords("Cardiovascular", 1, "circulation", "cardiac", "heart", "bp");
        RegisterKeywords("Cardiovascular", 2, "pressure", "rate");

        RegisterKeywords("Neurology", 0, "neurology", "neuro");
        RegisterKeywords("Neurology", 1, "neurological", "gcs", "sedation", "brain");
        RegisterKeywords("Neurology", 2, "conscious", "alert");

        RegisterKeywords("Renal & Fluids", 0, "renal");
        RegisterKeywords("Renal & Fluids", 1, "fluids", "fluid", "urine", "output", "kidney");
        RegisterKeywords("Renal & Fluids", 2, "intake");

        RegisterKeywords("Plan & Recommendation", 0, "plan");
        RegisterKeywords("Plan & Recommendation", 1, "recommendation", "tasks", "goals", "management", "todo");
    }

    void RegisterKeywords(string category, int priority, params string[] words)
    {
        foreach (var w in words) keywordMap[w.ToLower()] = (category, priority);
    }

    public void ProcessUtterance(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var words = text.ToLower().Split(new[] { ' ', '.', '?' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        string bestMatch = null;
        int bestPriority = 99;

        foreach (var word in words)
        {
            if (keywordMap.TryGetValue(word, out var entry))
            {
                if (entry.priority < bestPriority)
                {
                    bestPriority = entry.priority;
                    bestMatch = entry.title;
                    if (bestPriority == 0) break; // Can't beat priority 0
                }
            }
        }

        if (bestMatch != null && bestMatch != currentCategory)
        {
            currentCategory = bestMatch;
            panelTitleText.text = currentCategory;
            Debug.Log($"Context Switched: {currentCategory}");
        }
    }
}