using UnityEngine;

namespace SceneGenerator
{
    public class DMHelper : MonoBehaviour
    {
        public DataManager dm;
        private void Start()
        {
            dm.Init();
        }
    }

}
