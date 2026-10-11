using System;
using UnityEngine;

public class DesktopUI : MonoBehaviour
{
    [SerializeField] private UIButton mailButton;
    [SerializeField] private RectTransform mailList;
    [SerializeField] private UIButton mailTaskButton;
    [SerializeField] private RectTransform mailWritingView;
    [SerializeField] private UIButton sendMailButton;


    private void OnEnable()
    {
        MailTaskSetup();
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

}
