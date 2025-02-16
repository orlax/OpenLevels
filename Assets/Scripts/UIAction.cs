using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIAction : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public Image panel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        GlobalStates.interactable.OnStateChanged += UpdateLabel;
        GlobalStates.message.OnStateChanged += messageUpdated;
        panel.gameObject.SetActive(false);
    }

    // Update is called once per frame
    private void OnDisable()
    {
        GlobalStates.interactable.OnStateChanged -= UpdateLabel;
    }

    private void UpdateLabel(IInteractable interactable)
    {
        if (interactable == null)
        {
            panel.gameObject.SetActive(false);
            return;
        }

        messageText.text = interactable.label;
        panel.gameObject.SetActive(true);
    }

    //if the message is shown it means that the player is interacting with the object
    private void messageUpdated(string message)
    {
        panel.gameObject.SetActive(false);
        return;
    }
}
