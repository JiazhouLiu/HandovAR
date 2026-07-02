using UnityEngine;
using System.Collections;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
using UnityEngine.Windows.Speech;
#endif

public class VoiceRecognition : MonoBehaviour
{
    public ContextManager contextManager;
    private bool isRestarting;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
    private DictationRecognizer recognizer;
#endif

    void Start()
    {
        // check platform before init
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        InitRecognizer();
#else
        Debug.LogWarning("Windows.Speech is not supported on Android/Quest. Voice commands will not work on headset.");
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
    void InitRecognizer()
    {
        recognizer = new DictationRecognizer();

        recognizer.DictationHypothesis += (text) => 
        {
            contextManager.ProcessUtterance(text);
        };

        recognizer.DictationComplete += (cause) =>
        {
            if (cause != DictationCompletionCause.Complete)
            {
                Debug.LogWarning($"Dictation stopped: {cause}");
                Restart();
            }
        };

        recognizer.DictationError += (error, hresult) =>
        {
            Debug.LogError($"Dictation error: {error}");
            Restart();
        };

        recognizer.Start();
    }

    void Restart()
    {
        if (isRestarting) return;
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        isRestarting = true;
        
        if (recognizer != null)
        {
            recognizer.Dispose();
            recognizer = null;
        }

        yield return new WaitForSeconds(1f); // cooldown
        
        InitRecognizer();
        isRestarting = false;
    }

    void OnDestroy()
    {
        if (recognizer != null)
        {
            if (recognizer.Status == SpeechSystemStatus.Running)
                recognizer.Stop();
            
            recognizer.Dispose();
        }
    }
#else
    // dummy methods
    void Restart() { }
    IEnumerator RestartRoutine() { yield break; }
#endif
}