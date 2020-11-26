using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FitCamera : MonoBehaviour {

    public Camera targetCamera;

    public Vector3 AreaSize;

    // 90度回転
    public bool isRotateZ = false;

    void Fit()
    {
        var posViewport = new Vector3(0.5f, 0.5f, targetCamera.farClipPlane - targetCamera.nearClipPlane);
        transform.position = targetCamera.ViewportToWorldPoint(posViewport);
        var size = 2f * targetCamera.orthographicSize;

        if (isRotateZ)
        {
            transform.localScale = new Vector3(size, size * targetCamera.aspect, 1f);
            AreaSize = new Vector3(size, size * targetCamera.aspect, 1.0f);
        }
        else
        {
            transform.rotation = targetCamera.transform.rotation;
            transform.localScale = new Vector3(size * targetCamera.aspect, size, 1f);
            AreaSize = new Vector3(size * targetCamera.aspect, size, 1.0f);
        }
    }

    // Use this for initialization
    void Start () {
        Fit();
    }
	
	// Update is called once per frame
	void Update () {
        //Fit();
	}
}
