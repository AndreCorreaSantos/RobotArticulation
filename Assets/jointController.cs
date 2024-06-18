using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Joint{
    public string jointAxis;
    public GameObject jointObject;
    public Vector3 rotationAxis = Vector3.up; // Default to rotating around the Y-axis
}

public class jointController : MonoBehaviour
{
    public Joint[] joints;
    private float rotationSpeed = 45;

    // Update is called once per frame
    void Update()
    {
        foreach (Joint joint in joints){
            float input = Input.GetAxis(joint.jointAxis);
            if(Mathf.Abs(input) > 0){
                float degrees = 45;
                rotateJoint(degrees,joint,input);
            }
        }
    }

    void rotateJoint(float degrees, Joint joint, float input){
        float rotationAmount = input * 100 * Time.deltaTime * rotationSpeed;
        Quaternion rotation = Quaternion.Euler(joint.rotationAxis * rotationAmount);
        joint.jointObject.transform.rotation *= rotation;
    }
}

