using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SceneGenerator
{
    public class Outliner : MonoBehaviour
    {
        public Camera cam;

        public ObjectReference selectedObject;

        public Dropdown meshDropdown;

        public ObjectReference baseObject;

        public void SelectSubmodel(int input)
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            switch (input)
            {
                case 0:
                    if (Physics.Raycast(ray, out hit) && input == 0)
                    {

                        if (!EventSystem.current.IsPointerOverGameObject())
                        {
                            selectedObject.Value = hit.transform.gameObject;

                            ActivateOutline();
                            for (int i = 0; i < meshDropdown.options.Count; i++)
                            {
                                if (meshDropdown.options[i].text.Equals(selectedObject.Value.name))
                                {
                                    meshDropdown.value = i;
                                }
                            }
                        }
                    }
                    break;
                case 1:
                    meshDropdown.value++;
                    selectedObject.Value = baseObject.Value.transform.GetChild(0).Find(meshDropdown.options[meshDropdown.value].text).gameObject;
                    break;
                case 2:
                    meshDropdown.value--;
                    selectedObject.Value = baseObject.Value.transform.GetChild(0).Find(meshDropdown.options[meshDropdown.value].text).gameObject;
                    break;
            } 
        }

        public void OnDropdownSelect()
        {
            selectedObject.Value = baseObject.Value.transform.GetChild(0).Find(meshDropdown.options[meshDropdown.value].text).gameObject;

            ActivateOutline();       
        }


        public void ActivateOutline()
        {
            GameObject obj = selectedObject.Value;

            DisableOutlines();

            if (obj.GetComponent<Outline>() == null)
            {
                var outline = obj.AddComponent<Outline>();

                outline.OutlineMode = Outline.Mode.OutlineAll;
                outline.OutlineColor = Color.yellow;
                outline.OutlineWidth = 5f;
            }
            else obj.GetComponent<Outline>().enabled = true;
        }


        public void DisableOutlines()
        {
            Outline[] allOutlineComponents = FindObjectsOfType<Outline>();

            foreach(Outline o in allOutlineComponents)
            {
                o.enabled = false;
                
            }
        }


        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            { 
                SelectSubmodel(0);
            }

            if (Input.GetKeyUp("up"))
            {
                SelectSubmodel(1);
            }

            if (Input.GetKeyUp("down"))
            {
                SelectSubmodel(2);
            }
        }
    }

}