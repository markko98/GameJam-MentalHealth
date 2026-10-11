using TMPro;
using UnityEngine;

public class ClockView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI time;

    private EventBinding<TimeChangedEvent> timeChangedBinding;

    private void Awake()
    {
        timeChangedBinding = new EventBinding<TimeChangedEvent>(data => SetTime(data.time));
    }

    private void OnEnable()
    {
        UEventBus<TimeChangedEvent>.Register(timeChangedBinding);
    }

    private void OnDisable()
    {
        UEventBus<TimeChangedEvent>.Deregister(timeChangedBinding);
    }

    public void SetTime(string value)
    {
        time.text = value;
    }
}
