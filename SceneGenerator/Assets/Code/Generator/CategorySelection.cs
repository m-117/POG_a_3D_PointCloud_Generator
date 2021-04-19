using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SceneGenerator
{

    public class CategorySelection : MonoBehaviour
    {
        public StringRuntimeSet categories;

        public GameObject categoryPrefab;

        public void PopulateSelection()
        {
            GameObject newObj;

            for (int i = 0; i < categories.Items.Count; i++)
            {
                newObj = (GameObject)Instantiate(categoryPrefab, transform);
                newObj.GetComponentInChildren<Text>().text = categories.Items[i];
            }

            Destroy(categoryPrefab);
        }



        // Start is called before the first frame update
        void Start()
        {
            PopulateSelection();
        }

    }

}