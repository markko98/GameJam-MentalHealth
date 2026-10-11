public class ConnectionStatusEvent : IEvent
{
    public readonly bool isConnected;

    public ConnectionStatusEvent(bool isConnected)
    {
        this.isConnected = isConnected;
    }
}
public class SoundSettingsChangedEvent : IEvent
{
    public readonly bool musicMuted;
    public readonly bool soundMuted;
    public readonly float musicVolume;
    public readonly float soundVolume;

    public SoundSettingsChangedEvent(bool musicMuted, bool soundMuted, float musicVolume, float soundVolume)
    {
        this.musicMuted = musicMuted;
        this.soundMuted = soundMuted;
        this.musicVolume = musicVolume;
        this.soundVolume = soundVolume;
    }
}
public class CurrencyChangedEvent : IEvent
{
    public readonly WalletAccount account;
    public readonly CurrencyType type;
    public readonly int before;
    public readonly int after;
    public readonly int delta;
    public readonly CurrencySource source;
    public readonly string reason;
    public readonly string beforeFormatted;
    public readonly string afterFormatted;

    public CurrencyChangedEvent(
        WalletAccount account,
        CurrencyType type,
        int before,
        int after,
        int delta,
        CurrencySource source,
        string reason,
        string beforeFormatted,
        string afterFormatted)
    {
        this.account = account;
        this.type = type;
        this.before = before;
        this.after = after;
        this.delta = delta;
        this.source = source;
        this.reason = reason;
        this.beforeFormatted = beforeFormatted;
        this.afterFormatted = afterFormatted;
    }
}

public class ItemClickedEvent : IEvent
{
    public readonly string id;
    public readonly UnityEngine.GameObject source;

    public ItemClickedEvent(string id, UnityEngine.GameObject source)
    {
        this.id = id;
        this.source = source;
    }
}


public class ModeChangedEvent : IEvent
{
    public readonly GameMode mode;

    public ModeChangedEvent(GameMode mode)
    {
        this.mode = mode;
    }
}

public class GameOverEvent : IEvent
{
}

public class MailTaskDoneEvent : IEvent
{
}

public class MeetingTaskDoneEvent : IEvent
{
}