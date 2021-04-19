using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SceneGenerator
{
    public class WindowManager : MonoBehaviour
    {
        public GameObject[] elements;

        public void ToggleElement(int index)
        {
            elements[index].SetActive(!elements[index].activeSelf);
        }

        public void ActivateElement(int index)
        {
            elements[index].SetActive(true);
        }

        public void DeactivateElement(int index)
        {
            elements[index].SetActive(false);
        }

    }

}