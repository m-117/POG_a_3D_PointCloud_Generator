using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SceneGenerator
{
    public class ModelData
    {
        public ModelRuntimeSet Set;

        public string path { get; set; }
        public string[,] submeshTextures { get; set; }
        public float difficulty { get; set; }
        public Vector3 scale { get; set; }
        public string category { get; set; }

        //probably not needed

        //private void OnEnable()
        //{
        //    Set.Add(this);
        //}

        //private void OnDisable()
        //{
        //    Set.Remove(this);
        //}

    }
}

