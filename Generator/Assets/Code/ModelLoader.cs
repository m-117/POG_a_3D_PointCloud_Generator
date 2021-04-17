﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TriLib;
using UnityEngine;
using UnityMeshSimplifier;

namespace SceneGenerator
{
    public class ModelLoader : MonoBehaviour
    {
        private AssetLoaderOptions GetAssetLoaderOptions()
        {
            var assetLoaderOptions = AssetLoaderOptions.CreateInstance();
            assetLoaderOptions.DontLoadCameras = false;
            assetLoaderOptions.DontLoadLights = false;
            assetLoaderOptions.UseOriginalPositionRotationAndScale = true;
            assetLoaderOptions.DisableAlphaMaterials = true;
            assetLoaderOptions.MaterialShadingMode = MaterialShadingMode.Standard;
            assetLoaderOptions.AddAssetUnloader = true;
            assetLoaderOptions.AdvancedConfigs.Add(AssetAdvancedConfig.CreateConfig(AssetAdvancedPropertyClassNames.FBXImportDisableDiffuseFactor, true));
            return assetLoaderOptions;
        }

        public bool TryLoadObject(string filename, out GameObject model, Vector3 scale, Vector3 basePos)
        {
            model = null;
            if (string.IsNullOrEmpty(filename)) return false;
            if (!File.Exists(filename)) return false;
            var assetLoaderOptions = GetAssetLoaderOptions();
            using (var assetLoader = new AssetLoader())
            {
                try
                {
                    model = assetLoader.LoadFromFileWithTextures(filename, assetLoaderOptions);
                    DestroyEmptyChild(model);
                    AddMeshColliders(model);
                    ResizeModel(model.transform.GetChild(0).gameObject, scale);
                    AdjustModelPosition(model, basePos);
                    OptimizeModel(model);
                    if (assetLoader.MeshData == null || assetLoader.MeshData.Length == 0) return false;
                }
                catch (Exception)
                {
                    if (model != null)
                    {
                        Destroy(model);
                    }
                    Debug.Log("error");
                    return false;
                }
            }

            foreach (Camera camera in model.GetComponentsInChildren<Camera>())
            {
                Destroy(camera);
            }

            return true;
        }


        public bool LoadObjectWithMaterials(ModelData data, out GameObject model, Vector3 basePos)
        {
            if (TryLoadObject(data.path, out model, data.scale, basePos))
            {
                model.transform.localScale = data.scale;
                ApplyMaterials(data.submeshTextures, model);
                return true;
            }
            else return false;
        }

        public void ApplyMaterials(string[,] textures, GameObject model)
        {
            foreach(Transform childObject in model.transform.GetChild(0))
            {
                Mesh mesh = childObject.gameObject.GetComponent<MeshFilter>().mesh;

                for(int j = 0; j < mesh.subMeshCount; j++)
                {
                    childObject.GetComponent<MeshRenderer>().materials[j]
                        .SetTexture("_MainTex", Resources.Load(textures[childObject.GetSiblingIndex(),j]) as Texture2D);
                }
            }
        }

        public void AddMeshColliders(GameObject model)
        {
            foreach(Transform t in model.transform.GetChild(0))
            {
                t.gameObject.AddComponent<MeshCollider>();
            }
        }

        public Bounds CalculateLocalBounds(GameObject model)
        {
            Quaternion currentRotation = model.transform.rotation;
            model.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            Bounds bounds = new Bounds(model.transform.position, Vector3.zero);

            foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }

            Vector3 localCenter = bounds.center - model.transform.position;
            bounds.center = localCenter;
            Debug.Log("The local bounds of this model is " + bounds);

            model.transform.rotation = currentRotation;

            return bounds;
        }

        public void ResizeModel(GameObject model, Vector3 scale)
        {
            Bounds bounds = CalculateLocalBounds(model);

            float scalingFactor = Mathf.Max(Mathf.Max(scale.x / bounds.size.x, scale.y / bounds.size.y),
                scale.z / bounds.size.z);

            model.transform.localScale = new Vector3(scalingFactor, scalingFactor, scalingFactor);

        }

        public void AdjustModelPosition(GameObject model, Vector3 basePos)
        {
            Bounds bounds = CalculateLocalBounds(model);

            Vector3 newPos = new Vector3(basePos.x + bounds.center.x,
                basePos.y - bounds.center.y,
                basePos.z + bounds.center.z);
            

            model.transform.position = newPos;
        }

        public void OptimizeModel(GameObject model)
        {
            int triangleCount = 0;
            float triMean;

            foreach (Transform child in model.transform.GetChild(0))
            {
                if (child.gameObject.GetComponent<MeshFilter>() == null) continue;
                triangleCount += child.GetComponent<MeshFilter>().mesh.triangles.Length;
            }

            triMean = triangleCount / model.transform.GetChild(0).childCount;

            foreach (Transform child in model.transform.GetChild(0))
            {
                if (child.gameObject.GetComponent<MeshFilter>() == null) continue;
                SimplifyMesh(child, triMean);
            }
        }

        public void SimplifyMesh(Transform t, float mean)
        {
            MeshSimplifier meshSimplifier = new MeshSimplifier();
            float diff = t.GetComponent<MeshFilter>().mesh.triangles.Length / mean;
            float quality = 2/diff;
            Mesh originalMesh;

            if (t.GetComponent<MeshFilter>().mesh.triangles.Length / mean >= 3)
            {
                originalMesh = t.gameObject.GetComponent<MeshFilter>().mesh;
                meshSimplifier.Initialize(originalMesh);
                meshSimplifier.SimplifyMesh(quality);
                Mesh destMesh = meshSimplifier.ToMesh();
                t.gameObject.GetComponent<MeshFilter>().mesh = destMesh;
            }
        }

        public void DestroyEmptyChild(GameObject model)
        {
            foreach(Transform t in model.transform.GetChild(0))
            {
                if(t.gameObject.GetComponent<MeshFilter>() == null)
                {
                    t.parent = null;
                    Destroy(t.gameObject);
                }
            }
        }

    }
}

