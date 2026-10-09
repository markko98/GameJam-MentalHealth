using System;
using TMPro;
using UnityEngine;

public interface ISnackbarContent
{
    event Action SnackbarContentDisplayFinished;
    GameObject GetContent();
    void Initialize();
    void SetTitle(TextMeshProUGUI textComponent, string title = "");
}
