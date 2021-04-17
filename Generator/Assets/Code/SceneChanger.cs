using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneGenerator
{
    public class SceneChanger : MonoBehaviour
    {
        public string TargetScene;

        public void changeScene()
        {
            SceneManager.LoadScene("ImportScene");
        }

    }
}

