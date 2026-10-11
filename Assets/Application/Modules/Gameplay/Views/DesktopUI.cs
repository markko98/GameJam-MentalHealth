using System;
using UnityEngine;

public class DesktopUI : MonoBehaviour
{
    [SerializeField] private GameObject[] lockedScreenObjects;
    [SerializeField] private UIButton mailButton;
    [SerializeField] private RectTransform mailList;
    [SerializeField] private UIButton mailTaskButton;
    [SerializeField] private RectTransform mailWritingView;
    [SerializeField] private UIButton sendMailButton;
    [SerializeField] private ClockView clockView;
    
    [Header("Logo task")]
    [SerializeField] private UIButton logoButton;
    [SerializeField] private RectTransform logoDesignTask;
    [SerializeField] private UIButton logoPlaceholderButton;
    [SerializeField] private UIButton logoCreativeButton;

    [Header("Slack call")]
    [SerializeField] private RectTransform slackCallRoot;
    [SerializeField] private UIButton slackCallButton;
   
    private AudioHandle slackRingtoneHandle;

    private void Awake()
    {
        slackCallRoot.gameObject.SetActive(false);
        logoDesignTask.gameObject.SetActive(false);
        logoPlaceholderButton.Text = "Why am I busting my ass for this job when AI could replace me tomorrow?";
        logoCreativeButton.Text = "Finally, something creative to do. I could play with few different designs.";
    }

    private void OnEnable()
    {
        MailTaskSetup();
        LogoTaskSetup();
        slackCallButton.Register(AnswerSlackCall);
    }

    private void MailTaskSetup()
    {
        mailList.gameObject.SetActive(false);
        mailWritingView.gameObject.SetActive(false);
        mailButton.Register(OpenMailList);
        sendMailButton.Register(SendMail);
        mailTaskButton.Register(OpenWritingMail);
    }

    private void OnDisable()
    {
        MailTaskCleanup();
        LogoTaskCleanup();
        slackCallButton.Unregister(AnswerSlackCall);
    }

    public void HideLockedScreen()
    {
        foreach (GameObject lockedScreen in lockedScreenObjects)
        {
            lockedScreen.SetActive(false);
        }
    }

    public void ShowSlackCall()
    {
        slackCallRoot.gameObject.SetActive(true);
        slackRingtoneHandle = AudioService.Play(SoundIds.slack);
    }

    private void AnswerSlackCall()
    {
        slackRingtoneHandle.Stop();
        slackCallRoot.gameObject.SetActive(false);
        UEventBus<SlackCallAnsweredEvent>.Raise(new SlackCallAnsweredEvent());
    }
    
    #region Mail Task
    
    private void MailTaskCleanup()
    {
        mailButton.Unregister(OpenMailList);
        sendMailButton.Unregister(SendMail);
        mailTaskButton.Unregister(OpenWritingMail);
    }

    private void SendMail()
    {
        UEventBus<MailTaskDoneEvent>.Raise(new MailTaskDoneEvent());
        mailWritingView.gameObject.SetActive(false);
        mailList.gameObject.SetActive(false);
        mailButton.interactable = false;
    }
    private void OpenMailList()
    {
        mailList.gameObject.SetActive(true);
    }

    private void OpenWritingMail()
    {
        mailWritingView.gameObject.SetActive(true);
    }

    #endregion

    #region Logo Task

    public void HighlightLogoTask()
    {
        logoButton.SetHighlighted(true);
    }

    private void LogoTaskSetup()
    {
        logoButton.Register(OpenLogoTask);
        logoPlaceholderButton.Register(PickPlaceholderLogo);
        logoCreativeButton.Register(PickCreativeLogo);
    }

    private void LogoTaskCleanup()
    {
        logoButton.Unregister(OpenLogoTask);
        logoPlaceholderButton.Unregister(PickPlaceholderLogo);
        logoCreativeButton.Unregister(PickCreativeLogo);
    }

    private void OpenLogoTask()
    {
        logoDesignTask.gameObject.SetActive(true);
    }

    private void PickPlaceholderLogo() => FinishLogoTask(false);
    private void PickCreativeLogo() => FinishLogoTask(true);

    private void FinishLogoTask(bool creative)
    {
        logoDesignTask.gameObject.SetActive(false);
        logoButton.interactable = false;
        UEventBus<LogoTaskDoneEvent>.Raise(new LogoTaskDoneEvent(creative));
    }

    #endregion
}
