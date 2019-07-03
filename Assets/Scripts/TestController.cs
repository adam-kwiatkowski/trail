using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestController : MonoBehaviour {


    public GameObject TestCube;
    public GameObject TestCube2;
    public float Transmission = 0.1f;
    //[SerializeField]
    private GameObject _draggedPiece;
    private float _distance;
    private GameObject _parent;

	// Use this for initialization
	void Start () {
        _parent = new GameObject();
        //TestCube.transform.parent = _parent.transform;
        //TestCube2.transform.parent = _parent.transform;
    }

    // Update is called once per frame
    void Update () {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
                if (hit.collider != null && hit.collider.tag == "Block")
                {
                    //hit.collider.enabled = false;
                    //original:   _draggedPiece = hit.collider.gameObject;//.transform.position = new Vector3();
                    _draggedPiece = hit.collider.gameObject.transform.parent.gameObject;//.transform.position = new Vector3();
                    // rotation: _draggedPiece.transform.Rotate(0,90,0);
                    //_distance = hit.distance; //Camera.main.ScreenToWorldPoint(gameObject.transform.position).z;
                    //#2 changing pivot point
                    //Vector3 temporaryTransform = _draggedPiece.transform.position;
                    //temporaryTransform.y = temporaryTransform.y - _draggedPiece.transform.localScale.y;
                    //_distance = Vector3.Distance(temporaryTransform, ray.origin);
                    //#1 works perfect
                    //_distance = Vector3.Distance(_draggedPiece.transform.position, ray.origin);
                    Debug.Log("Yeah!");
                    //_parent.transform.position = new Vector3(0,0,5);
                    //_parent.transform.localScale = new Vector3(3, 3, 3);
                }
        }
        if (Input.GetMouseButtonUp(0) && _draggedPiece != null)
        {
            // tylko, jeśli coś aktualnie jest zaznaczone;
            Vector3 rounded = _draggedPiece.transform.position;
            rounded.x = Mathf.Round(rounded.x);
            rounded.z = Mathf.Round(rounded.z);
            _draggedPiece.transform.position = rounded;
            _draggedPiece.transform.DetachChildren();
            Destroy(_draggedPiece);
            _draggedPiece = null;
            //_parent.transform.localScale = new Vector3(1, 1, 1);
            //TestCube.transform.parent = null;
            //TestCube2.transform.parent = null;
        }
        if (_draggedPiece != null)
        {

            //#1
            /*
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            _distance = Vector3.Distance(_draggedPiece.transform.position, ray.origin);
            Vector3 targetPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _distance));
            _draggedPiece.transform.position = new Vector3(targetPosition.x, _draggedPiece.transform.position.y, targetPosition.z);
            */

            /* // EXPERIMENTS
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
            //Vector3 pointOnObjectCenter = new Vector3(_draggedPiece.transform.position.x, _draggedPiece.transform.position.y + (_draggedPiece.transform.localScale.y / 2), _draggedPiece.transform.position.z);
            //Vector3 pointOnObjectCenter = new Vector3(_draggedPiece.transform.position.x, _draggedPiece.transform.position.y, _draggedPiece.transform.position.z);
            _distance = Vector3.Distance(_draggedPiece.transform.position, ray.origin);
            //_distance = Vector3.Distance(pointOnObjectCenter, ray.origin);
            //_distance = Camera.main.WorldToScreenPoint(gameObject.transform.position).z;
            Vector3 targetPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _distance));
           // float tempDistance = Vector3.Distance(targetPosition, ray.origin);
           // targetPosition = Vector3.Lerp(targetPosition, ray.origin, 1 - (_distance/tempDistance));
            //targetPosition = targetPosition + new Vector3(0, 0, _draggedPiece.transform.position.y);

            // optional offset
            //targetPosition.z += 2;

            //_draggedPiece.transform.position = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);
            _draggedPiece.transform.position = new Vector3(targetPosition.x, _draggedPiece.transform.position.y, targetPosition.z);
            //_draggedPiece.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _distance));
            */


            //#2
            // this creates a horizontal plane passing through this object's center
            //Plane plane = new Plane(Vector3.up, _draggedPiece.transform.position);
            //alt
            Vector3 pointOnObjectCenter = new Vector3(_draggedPiece.transform.position.x, _draggedPiece.transform.position.y + (_draggedPiece.transform.localScale.y / 2), _draggedPiece.transform.position.z);
            //Debug.Log("Point on object center: " + pointOnObjectCenter);
            //Debug.Log("Child: " + _draggedPiece.transform.GetChild(0).transform.position);
            //Debug.Log("Child: " + _draggedPiece.transform.GetChild(1).transform.position);
            //Debug.Log("Child: " + _draggedPiece.transform.GetChild(2).transform.position);



            // variant a:
            /*
            Vector3 centerPoint = Vector3.zero;
            for (int i=0; i < _draggedPiece.transform.childCount; i++)
            {
                centerPoint += _draggedPiece.transform.GetChild(i).transform.localPosition;
            }
            centerPoint /= _draggedPiece.transform.childCount;
            Debug.Log(centerPoint);
            //centerPoint = _draggedPiece.transform.TransformPoint(pointOnObjectCenter);
            */

            // variant b:
            Bounds bounds = new Bounds(_draggedPiece.transform.GetChild(0).transform.localPosition, Vector3.zero);
            for (int i = 1; i < _draggedPiece.transform.childCount; i++)
            {
                bounds.Encapsulate(_draggedPiece.transform.GetChild(i).transform.localPosition);
            }
            Vector3 centerPoint = bounds.center;

            Plane plane = new Plane(Vector3.up, pointOnObjectCenter);
            // create a ray from the mousePosition
            Ray ray2 = Camera.main.ScreenPointToRay(Input.mousePosition);
            // plane.Raycast returns the distance from the ray start to the hit point
            float distance;
            if (plane.Raycast(ray2, out distance))
            {
                // some point of the plane was hit - get its coordinates
                Vector3 hitPoint = ray2.GetPoint(distance);
                hitPoint.y -= (_draggedPiece.transform.localScale.y / 2);
                hitPoint.x -= centerPoint.x;
                hitPoint.z -= centerPoint.z;
                _draggedPiece.transform.position = hitPoint;
                // use the hitPoint to aim your cannon
            }


        //Vector3 pointInWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
        //_draggedPiece.transform.position = new Vector3(pointInWorld.x * _distance, _draggedPiece.transform.position.y, _draggedPiece.transform.position.z);
    }
        //Vector3 currentMousePosition = Input.mousePosition;
        //Debug.Log("Mouse: " + currentMousePosition);
        //Vector3 currentCubePosition = TestCube.transform.position;//TestCube.GetComponent<Transform>().position;
        //Debug.Log(currentCubePosition);
        //Vector3 newPosition = new Vector3(currentCubePosition.x, currentCubePosition.y, currentCubePosition.z);
        //TestCube.transform.position = newPosition; // currentCubePosition;
        ////Vector3 newPosition = new Vector3(currentMousePosition.x, currentCubePosition.y, currentMousePosition.y) * Transmission;
        ////TestCube.transform.position = newPosition;
    }

    void OnGUI()
    {
        Vector3 p = new Vector3();
        Camera c = Camera.main;
        Event e = Event.current;
        Vector2 mousePos = new Vector2();

        // Get the mouse position from Event.
        // Note that the y position from Event is inverted.
        mousePos.x = e.mousePosition.x;
        mousePos.y = c.pixelHeight - e.mousePosition.y;

        p = c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, c.nearClipPlane));

        GUILayout.BeginArea(new Rect(20, 20, 250, 120));
        GUILayout.Label("Screen pixels: " + c.pixelWidth + ":" + c.pixelHeight);
        GUILayout.Label("Mouse position: " + mousePos);
        GUILayout.Label("World position: " + p.ToString("F3"));
        GUILayout.EndArea();
    }
}
