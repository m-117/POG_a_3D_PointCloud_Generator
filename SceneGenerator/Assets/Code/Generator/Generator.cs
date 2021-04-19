using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SceneGenerator
{
    public class Generator : MonoBehaviour
    {
        public StringRuntimeSet categories;

        public ModelRuntimeSet modelData;

        public StringRuntimeSet textures;

        public DataManager dataManager;

        public ModelLoader modelLoader;

        public Tracer tracer;

        public StringRuntimeSet chosenCategories;

        public IList<ModelData> models;

        public IList<GameObject> loadedModels;

        public Vector3 basePos;

        public int datasetSize;

        public bool randomDiff;

        public IntVariable diff;


        public void GenerateDataSet()
        {
            for(int i = 0; i < datasetSize; i++)
            {
                GenerateScene();

                tracer.TraceScene();

                tracer.ExportDataToCSV();
            }
        }

        public void GenerateScene()
        {
            DestroyAllLoadedModels();

            float remainingDifficulty = GetDifficulty();
            GameObject dummy;
            loadedModels = new List<GameObject>();

            foreach(ModelData m in modelData.Items)
            {
                foreach(string s in chosenCategories.Items)
                {
                    if (m.category.Equals(s))
                    {
                        models.Add(m);
                    }
                }
            }

            while(remainingDifficulty > 0)
            {
                ModelData model = models[Random.Range(0, models.Count)];

                float size = model.scale.x;

                Vector3 randomPosition = new Vector3(Random.Range(-2.5f + size / 2, 2.5f - size / 2), Random.Range(-0.5f, 0.5f), Random.Range(-2.5f + size / 2, 2.5f - size / 2));

                if (modelLoader.LoadObjectWithMaterials(model, out dummy, basePos))
                {
                    remainingDifficulty -= model.difficulty;
                    loadedModels.Add(dummy);
                }
                //models.Remove(model);
            }
            AddRigidbody();
        }

        public void ToggleCategory(Toggle toggle)
        {
            if (toggle.isOn)
            {
                chosenCategories.Add(toggle.GetComponentInChildren<Text>().text);
            }
            else
            {
                chosenCategories.Remove(toggle.GetComponentInChildren<Text>().text);
            }
        }

        public void DestroyAllLoadedModels()
        {
            if(loadedModels != null)
            {
                foreach (GameObject g in loadedModels)
                {
                    Destroy(g);
                }
            }

        }

        public void SetDatasetSize(InputField input)
        {
            int size = int.Parse(input.text);
            if (size < 1) size = 1;
            input.text = size.ToString();

            datasetSize = size;
        }

        public void SetDifficulty(Slider slider)
        {
            diff.Value = (int)slider.value;

            GameObject.Find("DiffTextSet").GetComponent<Text>().text = diff.Value.ToString();
        }

        public void SetResolution(InputField input)
        {
            int res = int.Parse(input.text);
            if (res < 50) res = 50;
            tracer.resolution = res;
            input.text = res.ToString();
        }

        public void ToggleRandDiff(Toggle toggle)
        {
            randomDiff = toggle.isOn;
        }

        public float GetDifficulty()
        {
            float difficulty = randomDiff ? Random.Range(1, 10) : diff.Value;
            tracer.perspectives = 6 - (int)(difficulty/2);
            difficulty = 10 * Mathf.Pow(1.7f, difficulty) / (1 + 0.2f*(tracer.perspectives-1));
            return difficulty;
        }

        public void AddRigidbody()
        {
            foreach(GameObject obj in loadedModels)
            {
                foreach(Transform t in obj.transform.GetChild(0))
                {
                    Destroy(t.GetComponent<MeshCollider>());
                }
                obj.AddComponent<BoxCollider>();
                Bounds bounds = modelLoader.CalculateLocalBounds(obj.transform.GetChild(0).gameObject);
                obj.GetComponent<BoxCollider>().size = bounds.size;
                obj.GetComponent<BoxCollider>().center = bounds.center;
                obj.AddComponent<Rigidbody>().useGravity = true;
            }
        }

        public void DestroyRigidbody()
        {
            foreach(GameObject obj in loadedModels)
            {
                bool gravity = obj.GetComponent<Rigidbody>().useGravity;
                obj.GetComponent<Rigidbody>().useGravity = !gravity;
                Destroy(obj.GetComponent<Rigidbody>());
                Destroy(obj.GetComponent<BoxCollider>());
                foreach (Transform t in obj.transform.GetChild(0))
                {
                    t.gameObject.AddComponent<MeshCollider>();
                }
            }
        }


        // Start is called before the first frame update
        void Start()
        {
            models = new List<ModelData>();
        }

    }

}