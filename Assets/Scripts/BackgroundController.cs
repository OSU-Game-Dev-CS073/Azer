using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BackgroundController : MonoBehaviour
{
    private float startPos;
    public GameObject cam;
    public float parallaxEffect;
    private float length;

    
    void Start()
    {
        // Keep the original starting position from the scene
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;

    }

    void FixedUpdate()
    {
        // Parallax movement - background moves slower than camera
        float distance = (cam.transform.position.x - startPos) * parallaxEffect;
        float movement = cam.transform.position.x * (1 - parallaxEffect);
        transform.position = new Vector3(startPos + distance, transform.position.y, transform.position.z);
       if (movement > startPos + length)
        {
            startPos += length;
        }
        else if (movement < startPos - length)
        {
            startPos -= length;
        }
    }
}