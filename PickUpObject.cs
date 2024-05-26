using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PickUpObject : MonoBehaviour
{
    public float pickUpRange = 5f; // Range within which objects can be picked up
    public Transform holdPoint; // Position where the held object will be placed
    private GameObject heldObject; // Reference to the currently held object
    public float pickUpForce = 150f; // Force applied when picking up an object
    public float minThrowForce = 1f; // Minimum throw force
    public float maxThrowForce = 100f; // Maximum throw force
    private float throwForce; // Current throw force
    private GameObject lookedAtObject; // Reference to the object currently looked at
    public TextMeshProUGUI pickUpPrompt; // UI prompt for picking up objects
    public float rotationSpeed = 100f; // Speed of rotation for the held object
    private Color originalEmissionColor; // To store the original emission color
    private bool isChargingThrow; // Flag to indicate if throw is being charged
    private float chargeStartTime; // Time when charging starts
    public float chargeDuration = 1f; // Time required to start charging the throw
    public Slider throwForceSlider; // Reference to the UI slider

    void Update()
    {
        CheckObjectLookedAt(); // Check if looking at an object to pick up

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null && lookedAtObject != null) // Check if there's a looked at object to pick up
            {
                PickUp(lookedAtObject); // Pick up the object
            }
            else if (heldObject != null)
            {
                chargeStartTime = Time.time; // Reset charge start time when 'E' is tapped again while holding
            }
        }

        if (heldObject != null && Input.GetKey(KeyCode.E))
        {
            if (!isChargingThrow && Time.time - chargeStartTime > chargeDuration)
            {
                isChargingThrow = true; // Start charging the throw
                throwForce = minThrowForce; // Reset throw force at the start of charging
            }
            else if (isChargingThrow)
            {
                // Continue charging throw force
                throwForce = Mathf.Clamp(throwForce + Time.deltaTime * (maxThrowForce - minThrowForce), minThrowForce, maxThrowForce);
                UpdateThrowForceSlider(); // Update the slider with the current throw force
            }
        }

        if (heldObject != null && Input.GetKeyUp(KeyCode.E))
        {
            if (isChargingThrow)
            {
                ThrowObject(); // Throw the object
                isChargingThrow = false; // Reset charging flag
                throwForceSlider.gameObject.SetActive(false); // Hide the slider
            }
            else
            {
                // If not charging, do nothing on key release to keep holding the object
            }
        }

        RotateHeldObject(); // Rotate the held object based on mouse movement
    }

    void CheckObjectLookedAt()
    {
        if (lookedAtObject != null)
        {
            SetEmissionColor(lookedAtObject, originalEmissionColor); // Restore the original emission color
            lookedAtObject = null;
            pickUpPrompt.gameObject.SetActive(false); // Hide the pick-up prompt
        }

        RaycastHit hit;
        Vector3 forward = transform.TransformDirection(Vector3.forward) * pickUpRange;
        Debug.DrawRay(transform.position, forward, Color.green); // Draw the ray in the scene view

        if (Physics.Raycast(transform.position, transform.forward, out hit, pickUpRange))
        {
            if (hit.collider.CompareTag("PickUp") || hit.collider.CompareTag("Box"))
            {
                lookedAtObject = hit.collider.gameObject; // Set the looked at object
                Renderer renderer = lookedAtObject.GetComponent<Renderer>();
                originalEmissionColor = renderer.material.GetColor("_EmissionColor");
                SetEmissionColor(lookedAtObject, Color.yellow); // Highlight the object with emission

                // Display the pick-up text
                PickUpObjectComponent pickUpComponent = lookedAtObject.GetComponent<PickUpObjectComponent>();
                if (pickUpComponent != null && pickUpComponent.pickUpData != null)
                {
                    pickUpPrompt.text = pickUpComponent.pickUpData.pickUpText;
                }
                else
                {
                    pickUpPrompt.text = "Press E to Pick Up";
                }

                pickUpPrompt.gameObject.SetActive(true); // Show the pick-up prompt
            }
        }
    }

    void PickUp(GameObject pickUpObject)
    {
        if (pickUpObject.GetComponent<Rigidbody>())
        {
            Rigidbody rb = pickUpObject.GetComponent<Rigidbody>();
            rb.useGravity = false; // Disable gravity while holding the object
            rb.drag = 10; // Increase drag to prevent excessive movement
            rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent rotation

            rb.transform.parent = holdPoint; // Parent the object to the hold point
            rb.transform.localPosition = Vector3.zero; // Ensure correct position
            rb.transform.localRotation = Quaternion.identity; // Ensure correct rotation

            heldObject = pickUpObject; // Set the held object
            chargeStartTime = Time.time; // Record the time when the object is picked up
            throwForceSlider.gameObject.SetActive(true); // Show the slider
            throwForceSlider.value = 0; // Reset the slider value
        }
    }

    void DropObject()
    {
        if (heldObject.GetComponent<Rigidbody>())
        {
            Rigidbody rb = heldObject.GetComponent<Rigidbody>();
            rb.useGravity = true; // Enable gravity
            rb.drag = 1; // Reset drag
            rb.constraints = RigidbodyConstraints.None; // Remove constraints

            heldObject.transform.parent = null; // Unparent the object

            heldObject = null; // Clear the held object reference
            isChargingThrow = false; // Reset charging flag
            throwForceSlider.gameObject.SetActive(false); // Hide the slider
        }
    }

    void ThrowObject()
    {
        if (heldObject.GetComponent<Rigidbody>())
        {
            Rigidbody rb = heldObject.GetComponent<Rigidbody>();
            rb.useGravity = true; // Enable gravity
            rb.drag = 1; // Reset drag
            rb.constraints = RigidbodyConstraints.None; // Remove constraints

            heldObject.transform.parent = null; // Unparent the object
            rb.AddForce(transform.forward * throwForce, ForceMode.Impulse); // Apply throw force

            heldObject = null; // Clear the held object reference
            throwForce = minThrowForce; // Reset throw force for next throw
        }
    }

    void RotateHeldObject()
    {
        if (heldObject != null)
        {
            float rotateX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime; // Calculate rotation around the Y-axis
            float rotateY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime; // Calculate rotation around the X-axis

            heldObject.transform.Rotate(Vector3.up, -rotateX, Space.World); // Rotate around the Y-axis
            heldObject.transform.Rotate(Vector3.right, rotateY, Space.World); // Rotate around the X-axis
        }
    }

    void SetEmissionColor(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.SetColor("_EmissionColor", color); // Set the emission color
            if (color != Color.black)
            {
                renderer.material.EnableKeyword("_EMISSION"); // Enable emission
            }
            else
            {
                renderer.material.DisableKeyword("_EMISSION"); // Disable emission
            }
        }
    }

    void UpdateThrowForceSlider()
    {
        float chargeProgress = (throwForce - minThrowForce) / (maxThrowForce - minThrowForce); // Calculate charge progress
        throwForceSlider.value = chargeProgress; // Update the slider value
    }
}
