using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Displays a mm:ss timer on a TextMeshPro UI text.
// Counts down from startSeconds, or counts up if countDown is false.
public class TextTimer : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float startSeconds = 60f;
    [SerializeField] private bool countDown = true;
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool useUnscaledTime = false;

    [SerializeField] private UnityEvent onTimerFinished;

    private float currentSeconds;
    private bool isRunning;
    private int lastDisplayedSeconds = -1;

    public float CurrentSeconds => currentSeconds;
    public bool IsRunning => isRunning;

    private void Awake()
    {
        ResetTimer();
        if (startOnAwake)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        float delta = Time.deltaTime;
        if (useUnscaledTime)
        {
            delta = Time.unscaledDeltaTime;
        }

        if (countDown)
        {
            currentSeconds -= delta;
            if (currentSeconds <= 0f)
            {
                currentSeconds = 0f;
                isRunning = false;
                UpdateText();
                onTimerFinished?.Invoke();
                return;
            }
        }
        else
        {
            currentSeconds += delta;
        }

        UpdateText();
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        if (countDown)
        {
            currentSeconds = startSeconds;
        }
        else
        {
            currentSeconds = 0f;
        }
        UpdateText();
    }

    private void UpdateText()
    {
        // Ceil on countdown so "00:01" shows until it actually hits zero.
        int totalSeconds;
        if (countDown)
        {
            totalSeconds = Mathf.CeilToInt(currentSeconds);
        }
        else
        {
            totalSeconds = Mathf.FloorToInt(currentSeconds);
        }

        // Only rebuild the string when the displayed second changes.
        if (totalSeconds == lastDisplayedSeconds)
        {
            return;
        }
        lastDisplayedSeconds = totalSeconds;

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}