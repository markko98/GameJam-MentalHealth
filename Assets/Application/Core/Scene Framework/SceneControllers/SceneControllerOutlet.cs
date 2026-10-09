using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneControllerOutlet : MonoBehaviour
{
    public Canvas canvas;
    public CanvasGroup canvasGroup;
    public bool shouldResetApp;

    private void OnValidate()
    {
        if (canvasGroup == null && canvas != null)
        {
            canvas.TryGetComponent<CanvasGroup>(out canvasGroup);
        }
    }

    private void Awake()
    {
        if (shouldResetApp && !AppDelegate.appStarted)
        {
            SceneManager.LoadScene(SceneNames.EntryPoint);
        }
        
        if (canvasGroup == null && canvas != null)
        {
            canvas.TryGetComponent<CanvasGroup>(out canvasGroup);
        }
    }
}
