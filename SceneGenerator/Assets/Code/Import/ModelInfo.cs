using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SceneGenerator
{
    public class ModelInfo : MonoBehaviour
    {
        public GameObject objName;
        public GameObject triangles;
        public GameObject submeshes;

        public GameObject childMeshes;
        public GameObject totalTriangles;
        public GameObject totalSubmeshes;

        public ObjectReference baseObject;
        public ObjectReference selectedObject;



        public void UpdateModelInfo()
        {
            objName.GetComponent<Text>().text = selectedObject.name;
            triangles.GetComponent<Text>().text = "" + selectedObject.Value.GetComponent<MeshFilter>().mesh.triangles.Length/3;
            submeshes.GetComponent<Text>().text = "" + selectedObject.Value.GetComponent<MeshFilter>().mesh.subMeshCount;

            childMeshes.GetComponent<Text>().text = "" + baseObject.Value.transform.GetChild(0).childCount;
            totalTriangles.GetComponent<Text>().text = "" + CalculateTriangleCount();
            totalSubmeshes.GetComponent<Text>().text = "" + CalculateSubmeshCount();
        }

        public int CalculateSubmeshCount()
        {
            int meshCount = 0;

            foreach(Transform child in baseObject.Value.transform.GetChild(0))
            {
                meshCount += child.GetComponent<MeshFilter>().mesh.subMeshCount;
            }
            return meshCount;
        }

        public int CalculateTriangleCount()
        {
            int triangleCount = 0;

            foreach(Transform child in baseObject.Value.transform.GetChild(0))
            {
                triangleCount += child.GetComponent<MeshFilter>().mesh.triangles.Length;
            }

            return triangleCount/3;
        }

    }

}