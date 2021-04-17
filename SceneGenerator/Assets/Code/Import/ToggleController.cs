using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SceneGenerator
{
    public class ToggleController : MonoBehaviour
    {
        public ObjectReference selectedObject;

        public IntVariable selectedSubmesh;

        public StringVariable selectedSize;

        public ToggleGroup submeshToggles;

        public ToggleGroup sizeToggles;

        public GameObject togglePrefab;

        // Start is called before the first frame update
        void Start()
        {
            PopulateToggleGroup();
            SetChosenSize();
        }

        void PopulateToggleGroup()
        {
            GameObject newObj;

            for (int i = 0; i < 10; i++)
            {
                newObj = (GameObject)Instantiate(togglePrefab, transform);
                newObj.GetComponentInChildren<Text>().text = "" + (i + 1);
            }

            Destroy(togglePrefab);
        }

        public void UpdateToggleGroup()
        {
            int submeshes = selectedObject.Value.GetComponent<MeshFilter>().mesh.subMeshCount;

            for(int i = 1; i < transform.childCount; i++)
            {
                if (i < submeshes)
                {
                    transform.GetChild(i).gameObject.SetActive(true);
                }
                else transform.GetChild(i).gameObject.SetActive(false);
            }
        }

        public void SetActiveSubmesh()
        {
            foreach(Toggle t in submeshToggles.ActiveToggles())
            {
                string s = t.GetComponentInChildren<Text>().text;
                selectedSubmesh.Value = int.Parse(s);
            }
        }

        public void SetChosenSize()
        {
            foreach (Toggle t in sizeToggles.ActiveToggles())
            {
                selectedSize.Value = t.GetComponentInChildren<Text>().text;
            }
        }

    }

}