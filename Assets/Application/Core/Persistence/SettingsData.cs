using System;

[Serializable]
public class SettingsData
{
    public bool musicMuted;
    public bool soundMuted;
    public bool vibrationMuted;
    public float musicVolume = 0.7f;
    public float soundVolume = 0.7f;
}
