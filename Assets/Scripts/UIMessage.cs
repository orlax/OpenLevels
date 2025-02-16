using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIMessage : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public Image panel;

    Coroutine coroutine;

    private void OnEnable()
    {
        GlobalStates.message.OnStateChanged += UpdateMessage;
        panel.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        GlobalStates.message.OnStateChanged -= UpdateMessage;
    }

    private void UpdateMessage(string message)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        if(string.IsNullOrEmpty(message))
        {
            panel.gameObject.SetActive(false);
            return;
        }

        messageText.text = message;
        // Optionally start a coroutine to clear the message after a few seconds
        panel.gameObject.SetActive(true);
        coroutine = StartCoroutine(ClearMessage());
    }

    private IEnumerator ClearMessage()
    {
        yield return new WaitForSeconds(5);
        GlobalStates.message.Value = "";
    }
}
