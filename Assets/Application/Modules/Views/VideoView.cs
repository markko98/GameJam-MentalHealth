using System;
using UnityEngine;
using UnityEngine.Video;
using Object = UnityEngine.Object;

// Plays the clip once, closes itself and calls onFinished after the view is gone.
public class VideoView : UIViewController
{
    private const string ResourcePath = "VideoView";

    private readonly VideoClip clip;
    private readonly Action onFinished;
    private VideoViewOutlet outlet;
    private RenderTexture texture;
    private bool finished;

    public VideoView(VideoClip clip, Transform viewport, UIStackNavigationController controller, Action onFinished) : base(controller)
    {
        this.clip = clip;
        this.onFinished = onFinished;
        view = Object.Instantiate(Resources.Load<GameObject>(ResourcePath), viewport, false);
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        outlet = view.GetComponentInChildren<VideoViewOutlet>(true);

        texture = new RenderTexture((int)clip.width, (int)clip.height, 0);
        var player = outlet.player;
        player.playOnAwake = false;
        player.isLooping = false;
        player.clip = clip;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = texture;
        outlet.screen.texture = texture;
    }

    public override void ViewDidAppear()
    {
        base.ViewDidAppear();
        outlet.player.loopPointReached += OnVideoEnded;
        outlet.player.Play();
    }

    public override void ViewWillDisappear()
    {
        base.ViewWillDisappear();
        outlet.player.loopPointReached -= OnVideoEnded;
    }

    public override void ViewDidDisappear()
    {
        base.ViewDidDisappear();
        texture.Release();
        Object.Destroy(texture);
        Object.Destroy(view);

        if (finished) return;
        finished = true;
        onFinished?.Invoke();
    }

    private void OnVideoEnded(VideoPlayer player)
    {
        RemoveView();
    }
}
