using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneGenerator
{
    public class SceneChanger : MonoBehaviour
    {
        public void ChangeScene(string target)
        {
            SceneManager.LoadScene(target);
        }
    }
}

