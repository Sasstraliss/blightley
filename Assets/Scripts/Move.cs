using UnityEngine;
using System.Collections;

public class Move : MonoBehaviour
{
    public float movementModifier;
    public float movementModifierDefault;
    public float rotationModifier;

    void Awake()
    {
        if (networkView.isMine)
        {
            movementModifierDefault = movementModifier;
        }
    }

    void Update()
    {
        if (networkView.isMine)
        {
            if (Input.GetAxis("Vertical") > 0.5f)
                rigidbody.AddRelativeForce((Vector3.forward * Input.GetAxis("Vertical") * movementModifier) * Time.deltaTime);
            else if (Input.GetAxis("Vertical") < -0.5f)
                rigidbody.AddRelativeForce((Vector3.forward * Input.GetAxis("Vertical") * (movementModifierDefault / 5)) * Time.deltaTime);
            rigidbody.AddRelativeTorque(new Vector3(0, (Input.GetAxis("Horizontal") * rotationModifier) * Time.deltaTime, 0));
        }
    }
}