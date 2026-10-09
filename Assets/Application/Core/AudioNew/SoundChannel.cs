// Decides which mixer group a sound is routed to and which volume setting controls it:
// Music + Ambience follow the music setting, Sfx + UI follow the sound setting.
public enum SoundChannel
{
    Sfx,
    UI,
    Music,
    Ambience
}
