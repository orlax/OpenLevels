using UnityEngine;
using DG.Tweening; // DOTween namespace

public class Door : MonoBehaviour, IInteractable
{
    public bool IsOpen = false;
    public string actionLabel = "(E) Open Door";
    public string label => actionLabel;

    public int priority => 1;

    // References to door parts (assign in Inspector)
    public Transform doorRotate;   // The part that rotates (default local rotation = 0)
    public Transform doorHandle;   // Optional handle that rotates on local X

    [Header("Door Open Settings")]
    public float openAngle = 90f;      // Maximum angle for door open rotation
    public float openDuration = 0.5f;  // Duration for door open/close animations

    [Header("Door Budge Settings")]
    public float budgeAngle = 5f;      // Small angle for budge animation
    public float budgeDuration = 0.3f; // Total duration for budge (out & back)

    [Header("Handle Settings")]
    public float handleAngle = 45f;    // Angle for the handle to rotate (on local X)
    public float handleDuration = 0.5f; // Duration for handle rotation

    [Header("Auto Close Settings")]
    public float autoCloseDistance = 5f; // Distance beyond which an open door auto-closes

    // Reference to the player's transform (set on trigger; not cleared on exit)
    private Transform playerTransform;

    // Flags to prevent overlapping animations
    private bool isAnimating = false;

    // Store initial rotations so we can return to them on close
    private Vector3 initialDoorRotation;
    private Vector3 initialHandleRotation;

    // Keep track of which way the door opened (for a proper close animation)
    private float currentTargetAngle;

    private void Start()
    {
        if (doorRotate != null)
            initialDoorRotation = doorRotate.localEulerAngles;
        else
            Debug.LogWarning("DoorRotate transform is not assigned!");

        if (doorHandle != null)
            initialHandleRotation = doorHandle.localEulerAngles;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GlobalStates.SetInteractable(this);
            playerTransform = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GlobalStates.interactable.Value = null;
            // Do not null the playerTransform so we can track distance for auto-closing.
        }
    }

    private void Update()
    {
        // Auto-close the door if it's open and the player has moved far enough away.
        if (IsOpen && playerTransform != null && !isAnimating)
        {
            float distance = Vector3.Distance(playerTransform.position, transform.position);
            if (distance > autoCloseDistance)
            {
                AnimateDoorClose();
            }
        }
    }

    public void Interact()
    {
        if (isAnimating)
            return; // Prevent overlapping animations

        if (playerTransform == null)
        {
            Debug.LogWarning("Player transform not found for door interaction.");
            return;
        }

        if (!IsOpen)
        {
            GlobalStates.message.Value = "La Puerta esta Cerrada";
            AnimateDoorBudge();
        }
        else
        {
            AnimateDoorOpen();
        }
    }

    /// <summary>
    /// Animates the door opening away from the player.
    /// The door rotates around its Y axis, and the handle rotates on its local X axis.
    /// </summary>
    private void AnimateDoorOpen()
    {
        isAnimating = true;

        // Determine the direction: convert the player's position into doorRotate's local space.
        Vector3 localPlayerPos = doorRotate.InverseTransformPoint(playerTransform.position);
        // If the player is on the right (local X >= 0), open left (negative angle); otherwise, open right.
        float directionSign = localPlayerPos.x >= 0 ? -1f : 1f;
        currentTargetAngle = openAngle * directionSign;

        Sequence seq = DOTween.Sequence();

        // Animate the door rotating on the Y axis.
        seq.Append(doorRotate.DOLocalRotate(
            new Vector3(0, currentTargetAngle, 0), 
            openDuration, 
            RotateMode.Fast)
            .SetEase(Ease.OutCubic));

        // Animate the handle rotating on its local X axis (if assigned).
        if (doorHandle != null)
        {
            seq.Join(doorHandle.DOLocalRotate(
                new Vector3(handleAngle, 0, 0), 
                handleDuration, 
                RotateMode.Fast)
                .SetEase(Ease.OutCubic));
        }

        seq.OnComplete(() =>
        {
            IsOpen = true;
            isAnimating = false;
        });
    }

    /// <summary>
    /// Animates the door closing (returning to its original rotation).
    /// </summary>
    private void AnimateDoorClose()
    {
        isAnimating = true;

        Sequence seq = DOTween.Sequence();

        // Animate door rotating back to its initial rotation.
        seq.Append(doorRotate.DOLocalRotate(
            initialDoorRotation, 
            openDuration, 
            RotateMode.Fast)
            .SetEase(Ease.OutCubic));

        // Animate door handle returning to its initial rotation.
        if (doorHandle != null)
        {
            seq.Join(doorHandle.DOLocalRotate(
                initialHandleRotation, 
                handleDuration, 
                RotateMode.Fast)
                .SetEase(Ease.OutCubic));
        }

        seq.OnComplete(() =>
        {
            isAnimating = false;
        });
    }

    /// <summary>
    /// Plays a small "budge" animation when the door is closed.
    /// The door quickly rotates a little and then returns to its original rotation.
    /// </summary>
    private void AnimateDoorBudge()
    {
        isAnimating = true;

        Vector3 localPlayerPos = doorRotate.InverseTransformPoint(playerTransform.position);
        float directionSign = localPlayerPos.x >= 0 ? -1f : 1f;
        float targetBudgeAngle = budgeAngle * directionSign;

        Sequence seq = DOTween.Sequence();

        // Budge out
        seq.Append(doorRotate.DOLocalRotate(
            new Vector3(0, targetBudgeAngle, 0),
            budgeDuration / 2f, 
            RotateMode.Fast)
            .SetEase(Ease.OutCubic));

        // Budge back to the initial rotation.
        seq.Append(doorRotate.DOLocalRotate(
            initialDoorRotation, 
            budgeDuration / 2f, 
            RotateMode.Fast)
            .SetEase(Ease.InCubic));

        seq.OnComplete(() =>
        {
            isAnimating = false;
        });
    }
}
