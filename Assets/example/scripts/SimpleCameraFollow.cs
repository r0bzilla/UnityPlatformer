using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    private Camera cam;

    public Transform target;
    public float speed = 2;


    protected void Start()
    {
        this.cam = GetComponent<Camera>();
    }


    protected void FixedUpdate()
    {
        Vector3 newPos = Vector3.Lerp(this.cam.transform.position, this.target.position, Time.fixedDeltaTime * speed);
        newPos.z = cam.transform.position.z;
        this.cam.transform.position = newPos;
    }
}
