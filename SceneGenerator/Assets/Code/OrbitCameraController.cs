using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SceneGenerator
{
    public class OrbitCameraController : MonoBehaviour
    {

        [SerializeField] private Camera cam;
        [SerializeField] public Transform target;
        [SerializeField] private float distanceToTarget = 1.5f;

        float rotSpeed = 5;

        private Vector3 previousPosition;

        //void OnMouseDrag()
        //{
        //    float rotX = Input.GetAxis("Mouse X") * rotSpeed * Mathf.Deg2Rad;
        //    float rotY = Input.GetAxis("Mouse Y") * rotSpeed * Mathf.Deg2Rad;

        //    target.RotateAround(Vector3.up, -rotX);
        //    target.RotateAround(Vector3.right, rotY);
        //}

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
            }
            else if (Input.GetMouseButton(1))
            {
                distanceToTarget -= Input.mouseScrollDelta.y;

                Vector3 newPosition = cam.ScreenToViewportPoint(Input.mousePosition);
                Vector3 direction = previousPosition - newPosition;

                float rotationAroundYAxis = direction.x * 180 * rotSpeed; // camera moves horizontally
                float rotationAroundXAxis = -direction.y * 180 * rotSpeed; // camera moves vertically

                //float rotationAroundYAxis = Input.GetAxis("Mouse Y") * rotSpeed * Mathf.Deg2Rad;
                //float rotationAroundXAxis = Input.GetAxis("Mouse X") * rotSpeed * Mathf.Deg2Rad;

                //target.position = cam.transform.position;

                target.transform.Translate(new Vector3(0, 0, Input.mouseScrollDelta.y), Space.World);

                target.transform.Rotate(Vector3.down, rotationAroundYAxis);
                target.transform.Rotate(Vector3.right, rotationAroundXAxis);

                //target.transform.Rotate(rotationAroundXAxis, rotationAroundYAxis, 0);


                previousPosition = newPosition;
            }

            if (Input.GetMouseButton(2))
            {


                Vector3 translation = new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"), 0);

                target.transform.Translate(translation, Space.World);
            }
        }
    }

}
