using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interact : MonoBehaviour
{
    public PlayerInput playerInput;
    public bool isBusy = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInput.actions["Interact"].performed += InteractWithObject;        
    }

    private void InteractWithObject(InputAction.CallbackContext context)
    {
        if(GlobalStates.interactable.Value!=null && isBusy==false){
            GlobalStates.interactable.Value.Interact();
            isBusy = true;
            StartCoroutine(Release());
        }
    }

    private IEnumerator Release()
    {
        yield return new WaitForSeconds(2);
        isBusy = false;
    }

}
