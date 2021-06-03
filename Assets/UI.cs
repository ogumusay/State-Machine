using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI : MonoBehaviour
{    
    Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        transform.LookAt(cam.transform);
    }
}
