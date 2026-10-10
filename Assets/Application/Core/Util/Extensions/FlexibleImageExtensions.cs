using System.Threading.Tasks;
using JeffGrawAssets.FlexibleUI;
using UnityEngine;

// Plays the animation states authored on a FlexibleImage from code. The five slots are the ones the
// Inspector labels Normal..Disabled; in Script mode the names mean nothing, so pick a convention
// (0 = closed, 1 = open) and keep it.
//
//   outlet.screen.PlayState(FlexibleState.Highlighted);            // unlock the screen
//   await outlet.eyelids.PlayStateAsync(FlexibleState.Highlighted); // blink shut, then continue
public enum FlexibleState
{
    Normal,
    Highlighted,
    Pressed,
    Selected,
    Disabled
}

public static class FlexibleImageExtensions
{
    private const int LastState = 4;

    public static void PlayState(this FlexibleImage image, FlexibleState state) => image.PlayState((int)state);

    public static void PlayState(this FlexibleImage image, int state)
    {
        if (image == null) return;

        image.animationMode = FlexibleImage.AnimationStateDrivenBy.Script;
        image.scriptDrivenAnimationState = Mathf.Clamp(state, 0, LastState);
    }

    /// <summary>Plays the state, then waits out the duration authored on it.</summary>
    public static Task PlayStateAsync(this FlexibleImage image, FlexibleState state) => image.PlayStateAsync((int)state);

    public static async Task PlayStateAsync(this FlexibleImage image, int state)
    {
        image.PlayState(state);

        var seconds = image.StateDuration(state);
        if (seconds > 0f) await Task.Delay(Mathf.RoundToInt(seconds * 1000f));
    }

    public static float StateDuration(this FlexibleImage image, FlexibleState state) => image.StateDuration((int)state);

    /// <summary>Total time of the state's substates, taken from the slowest quad.</summary>
    public static float StateDuration(this FlexibleImage image, int state)
    {
        if (image == null) return 0f;

        state = Mathf.Clamp(state, 0, LastState);
        var quads = image.ActiveQuadDataContainer;
        var longest = 0f;

        for (var i = 0; i < quads.Count; i++)
        {
            var substates = quads[i].proceduralAnimationStates[state].proceduralProperties;
            if (substates == null) continue;

            var total = 0f;
            foreach (var substate in substates) total += substate.duration;
            if (total > longest) longest = total;
        }

        return longest;
    }
}
