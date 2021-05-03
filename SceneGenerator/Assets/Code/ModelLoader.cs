using System;
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

        public bool TryLoadObject(string filename, out GameObject model, Vector3 scale, Vector3 basePos, string category)
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
                    AddLabels(model, category);
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
            if (TryLoadObject(data.path, out model, data.scale, basePos, data.category))
            {
                model.transform.localScale = data.scale;
                ApplyMaterials(data.submeshTextures, model);
                RandomPosition(model);
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
                    string tex = textures[childObject.GetSiblingIndex(), j];

                    if(tex != null)
                    {
                        byte[] bytes = File.ReadAllBytes(tex);
                        Texture2D texture = new Texture2D(1, 1);
                        texture.LoadImage(bytes);
                        texture.Apply();

                        childObject.GetComponent<MeshRenderer>().materials[j]
                        .SetTexture("_MainTex", texture);
                    }
                    else
                    {
                        childObject.GetComponent<MeshRenderer>().materials[j].SetColor("_MainTex", Color.white);
                    }
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

        public void AddLabels(GameObject model, string label)
        {
            foreach(Transform t in model.transform.GetChild(0))
            {
                t.gameObject.AddComponent<Label>();
                t.gameObject.GetComponent<Label>().label = label;
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

            model.transform.rotation = currentRotation;

            return bounds;
        }

        public void ResizeModel(GameObject model, Vector3 scale)
        {
            Bounds bounds = CalculateLocalBounds(model);

            float scalingFactor = 1;

            if(bounds.size.x < scale.x && bounds.size.y < scale.y && bounds.size.z < scale.z)
            {
                scalingFactor = scale.x / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            }else
            {
                scalingFactor = Mathf.Min(scale.x / bounds.size.x, scale.y / bounds.size.y,
                    scale.z / bounds.size.z);
            }


            model.transform.localScale = new Vector3(scalingFactor, scalingFactor, scalingFactor);

        }

        public void RandomPosition(GameObject model)
        {
            Bounds bounds = CalculateLocalBounds(model);

            Vector3 randPos = new Vector3(UnityEngine.Random.Range(-2.5f + (bounds.size.x), 2.5f - (bounds.size.x)),
                UnityEngine.Random.Range(-1.5f + (bounds.size.y), 1.5f - (bounds.size.y)),
                UnityEngine.Random.Range(-2.5f + (bounds.size.z), 2.5f - (bounds.size.z)));

            model.transform.Translate(randPos);
            model.transform.rotation = UnityEngine.Random.rotation;
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

