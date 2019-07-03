using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour {

    public bool Rotable;
    public bool Reflectable;
    public Vector3 CenterPoint;
    public Vector3 PalettePosition;

    public void SetCentralPosition(Vector3 position)
    {
        CalculateCenterPoint();
        //Debug.Log(CenterPoint);
        position.x -= CenterPoint.x;
        position.z -= CenterPoint.z;
        transform.position = position;
        //transform.position = new Vector3(position.x - CenterPoint.x, position.y, position.z - CenterPoint.z);
    }

	// Use this for initialization
	void Start () {
        //_calculateCenterPoint();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void CalculateCenterPoint()
    {
        Bounds bounds = new Bounds(transform.GetChild(0).transform.localPosition, Vector3.zero);
        for (int i = 1; i < transform.childCount; i++)
        {
            bounds.Encapsulate(transform.GetChild(i).transform.localPosition);
        }
        CenterPoint = transform.rotation * bounds.center;
        CenterPoint = Vector3.Scale(CenterPoint, transform.localScale);
        //CenterPoint = transform.rotation * CenterPoint;
    }
}
