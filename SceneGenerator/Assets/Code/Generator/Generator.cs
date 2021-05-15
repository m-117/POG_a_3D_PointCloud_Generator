using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SceneGenerator
{
    public class Generator : MonoBehaviour
    {
        /// <summary>
        /// model database
        /// </summary>
        public ModelRuntimeSet modelData;

        /// <summary>
        /// components for load and trace ops
        /// </summary>
        public ModelLoader modelLoader;
        public IList<ModelData> models;
        public IList<GameObject> loadedModels;
        public Tracer tracer;
        public Vector3 basePos;
        public int usedDiff;

        /// <summary>
        /// user inputs
        /// </summary>
        public int datasetSize;
        public bool randomDiff;
        public IntVariable diff;
        public IntVariable noise;
        public StringRuntimeSet chosenCategories;

        /// <summary>
        /// scene info fields
        /// </summary>
        public Text modelCount;
        public Text meshCount;
        public Text difficulty;
        public Text triangleCount;



        public void GenerateDataSet()
        {
            StartCoroutine(SetCoroutine());
        }

        /// <summary>
        /// Dataset generation coroutine. Generate, trace and save scenes.
        /// Waiting times are slightly exaggerated to avoid errors.
        /// </summary>
        IEnumerator SetCoroutine()
        {
            for (int i = 0; i < datasetSize; i++)
            {
                GenerateScene();
                yield return new WaitForSecondsRealtime(18);
                tracer.TraceScene();
                yield return new WaitForSecondsRealtime(4);
                tracer.ExportDataToCSV();
                yield return new WaitForSecondsRealtime(4);
            }
        }

        /// <summary>
        /// Generate a single Scene. 
        /// </summary>
        public void GenerateScene()
        {
            DestroyAllLoadedModels();

            // init class counter
            Dictionary<string, int> counter = new Dictionary<string, int>();

            foreach(string s in chosenCategories.Items)
            {
                counter.Add(s, 0);
            }

            float remainingDifficulty = GetDifficulty();

            // init and fill list with suitable models
            loadedModels = new List<GameObject>();

            foreach (ModelData m in modelData.Items)
            {
                foreach(string s in chosenCategories.Items)
                {
                    if (m.category.Equals(s) /*&& m.difficulty <= remainingDifficulty*/)
                    {
                        models.Add(m);
                    }
                }
            }

            // fill scene until target difficulty is reached
            while(remainingDifficulty >= 0)
            {
                ModelData model = models[Random.Range(0, models.Count)];

                string cat = model.category;
                float size = model.scale.x;

                if ((cat.Equals("Bookshelf_4") || cat.Equals("Chair_5") || cat.Equals("Desk_6")) && counter[cat] >= (usedDiff / 3)) continue;

                Vector3 randomPosition = new Vector3(Random.Range(-2.5f + size, 2.5f - size), Random.Range(-0.5f, 0.5f), Random.Range(-2.5f + size, 2.5f - size));

                if (modelLoader.LoadObjectWithMaterials(model, out GameObject dummy, basePos))
                {
                    //remainingDifficulty -= model.difficulty;
                    remainingDifficulty -= model.scale.x;
                    counter[cat]++;
                    foreach(Transform child in dummy.transform.GetChild(0))
                    {
                        child.GetComponent<Label>().counter = counter[model.category];
                    }
                    loadedModels.Add(dummy);
                }
            }
            
            StartCoroutine(ApplyGravity());

            UpdateSceneInfo(loadedModels);
        }

        /// <summary>
        /// Add/Remove categories for scene generation
        /// </summary>
        /// <param name="toggle"> Target Category </param>
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

        /// <summary>
        /// Remove all loaded models from current scene 
        /// </summary>
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

        /// <summary>
        /// Set size of generated dataset.
        /// </summary>
        /// <param name="input"> User Input </param>
        public void SetDatasetSize(InputField input)
        {
            int size = int.Parse(input.text);
            if (size < 1) size = 1;
            input.text = size.ToString();

            datasetSize = size;
        }

        /// <summary>
        /// Read and set scene difficulty
        /// </summary>
        /// <param name="slider"> User Input </param>
        public void SetDifficulty(Slider slider)
        {
            diff.Value = (int)slider.value;

            GameObject.Find("DiffTextSet").GetComponent<Text>().text = diff.Value.ToString();
        }

        /// <summary>
        /// Set value for random jittering of trace rays.
        /// </summary>
        /// <param name="slider"> User Input </param>
        public void SetNoise(Slider slider)
        {
            noise.Value = (int)slider.value;
        }

        /// <summary>
        /// Set trace resolutions (X times X rays per perspective)
        /// </summary>
        /// <param name="input"> User Input </param>
        public void SetResolution(InputField input)
        {
            int res = int.Parse(input.text);
            if (res < 50) res = 50;
            tracer.resolution = res;
            input.text = res.ToString();
        }

        /// <summary>
        /// Toggle random scene difficulty
        /// </summary>
        /// <param name="toggle"> User Input </param>
        public void ToggleRandDiff(Toggle toggle)
        {
            randomDiff = toggle.isOn;
        }

        /// <summary>
        /// Get scene difficulty randomly or from user input. Difficulty is influenced by the number of tracing perspectives. 
        /// </summary>
        /// <returns> Scene difficulty </returns>
        public float GetDifficulty()
        {
            float difficulty = randomDiff ? Random.Range(1, 10.49f) : diff.Value;
            usedDiff = (int)difficulty;

            tracer.perspectives = 6 - (int)(usedDiff/3);
            //difficulty = (5 * difficulty) / (1 + 0.15f*(6 - tracer.perspectives));
            return usedDiff*2f;
        }

        /// <summary>
        /// Apply gravity to models in scene. Exchanges collider components and adds rigidbody components.
        /// Colliders are reverted and rigidbody components deleted after delay to freeze scene for tracing.
        /// </summary>
        IEnumerator ApplyGravity()
        {
            foreach (GameObject obj in loadedModels)
            {
                foreach (Transform t in obj.transform.GetChild(0))
                {
                    Destroy(t.GetComponent<MeshCollider>());
                    t.gameObject.AddComponent<BoxCollider>();
                }
                obj.AddComponent<Rigidbody>().useGravity = true;
            }

            yield return new WaitForSecondsRealtime(8);


            foreach (GameObject obj in loadedModels)
            {
                bool gravity = obj.GetComponent<Rigidbody>().useGravity;
                obj.GetComponent<Rigidbody>().useGravity = !gravity;
                Destroy(obj.GetComponent<Rigidbody>());

                foreach (Transform t in obj.transform.GetChild(0))
                {
                    Destroy(t.gameObject.GetComponent<BoxCollider>());
                }
            }

            yield return new WaitForSecondsRealtime(4);

            foreach (GameObject obj in loadedModels)
            {
                foreach (Transform t in obj.transform.GetChild(0))
                {
                    t.gameObject.AddComponent<MeshCollider>();
                }
            }

        }

        /// <summary>
        /// Calculates and displays scene stats.
        /// </summary>
        /// <param name="loadedModels"> Models in current scene </param>
        public void UpdateSceneInfo(IList<GameObject> loadedModels)
        {
            modelCount.text = loadedModels.Count.ToString();

            int meshes = 0;
            int triangles = 0;

            foreach(GameObject g in loadedModels)
            {
                meshes += g.transform.GetChild(0).childCount;

                foreach(Transform child in g.transform.GetChild(0))
                {
                    triangles += child.GetComponent<MeshFilter>().mesh.triangles.Length/3;
                }
            }

            triangleCount.text = triangles.ToString();

            meshCount.text = meshes.ToString();

            difficulty.text = usedDiff.ToString();

        }


        // Start is called before the first frame update
        void Start()
        {
            models = new List<ModelData>();
        }

    }

}