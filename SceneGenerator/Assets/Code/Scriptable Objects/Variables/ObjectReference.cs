using UnityEngine;

namespace SceneGenerator
{
    [CreateAssetMenu]
    public class ObjectReference : ScriptableObject
    {
        [SerializeField]
        private GameObject value;

        public GameObject Value
        {
            get { return value; }
            set { this.value = value; }
        }
    }
}
