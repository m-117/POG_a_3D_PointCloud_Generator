using UnityEngine;

namespace SceneGenerator
{
    public class ExitGame : MonoBehaviour
    {
        public void CloseApplication()
        {
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }

}