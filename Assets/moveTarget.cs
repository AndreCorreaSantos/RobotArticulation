using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveTarget : MonoBehaviour
{

    public float speed = 5.0f;
    void Update()
    {
        float inputX = Input.GetAxis("XTarget");
        float inputY = Input.GetAxis("YTarget");
        if(Mathf.Abs(inputX) > 0){
            float delta = inputX * speed*Time.deltaTime;
            transform.position += new Vector3(delta,0,0);
        }
        if(Mathf.Abs(inputY) > 0){
            float delta = inputY * speed*Time.deltaTime;
            transform.position += new Vector3(0,delta,0);
        }

    }
}
