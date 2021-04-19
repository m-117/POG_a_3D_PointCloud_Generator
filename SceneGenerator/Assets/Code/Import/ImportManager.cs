using SimpleFileBrowser;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SceneGenerator
{
    public class ImportManager : MonoBehaviour
    {
        public StringRuntimeSet texDataTypes;

        public Button changeDirectoryButton;

        public StringRuntimeSet modelDataTypes;

        public StringRuntimeSet categories;

        public ModelRuntimeSet modelData;

        public StringRuntimeSet textures;

        public DataManager dataManager;

        public ModelLoader modelLoader;

        public GameObject importObject;

        public ObjectReference baseObject;

        public ObjectReference selectedObject;

        public Camera importCam;

        public Vector3 basePosition;

        public GameObject meshDropdown;

        public Dropdown categoryDropdown;

        public IntVariable selectedSubmesh;

        public StringVariable importDirectoryPath;

        public ModelInfo info;

        public StringVariable selectedSize;

        public GameObject childMeshes;
        public GameObject totalTriangles;
        public GameObject totalSubmeshes;

        IList<string> importModelPaths;

        string[,] submeshTextureList;

        int modelIndex;


        public void ChooseDirectory()
        {
            FileBrowser.AddQuickLink("Users", "C:\\Users", null);

            // Show a select folder dialog 
            // onSuccess event: print the selected folder's path
            // onCancel event: print "Canceled"
            // Load file/folder: folder, Allow multiple selection: false
            // Initial path: default (Documents), Initial filename: empty
            // Title: "Select Folder", Submit button text: "Select"
            FileBrowser.ShowLoadDialog((paths) =>
            {
                importDirectoryPath.Value = FileBrowser.Result[0];
                changeDirectoryButton.GetComponentInChildren<Text>().text = "Import folder:\n" + importDirectoryPath.Value;
                ParseDirectory();
            },
                                      () => { Debug.Log("Canceled"); /**back to menu**/},
                                      FileBrowser.PickMode.Folders, false, null, null, "Select Folder", "Select");
        }


        public void ParseDirectory()
        {
            importModelPaths = new List<string>();

            foreach (string file in Directory.GetFiles(importDirectoryPath.Value))
            {
                string fileType = file.Split('.')[file.Split('.').Length - 1];

                if (texDataTypes.Items.Contains(fileType))
                {
                    dataManager.MoveFile(file, dataManager.texPath.Value);
                }
                else if (modelDataTypes.Items.Contains(fileType))
                {
                    importModelPaths.Add(file);
                }
            }
            modelIndex = 0;
            LoadNextModel();
        }

        public void LoadNextModel()
        {
            GameObject model;

            if(modelLoader.TryLoadObject(importModelPaths[modelIndex], out model, GetScale(), basePosition, ""))
            {
                Destroy(baseObject.Value);
                submeshTextureList = new string[model.transform.GetChild(0).childCount, 12];
                baseObject.Value = model;
                selectedObject.Value = model.transform.GetChild(0).GetChild(0).gameObject;
                UpdateMeshDropdown();
                UpdateCategoryDropdown();
                info.UpdateModelInfo();
            }
            else
            {
                importModelPaths.Remove(importModelPaths[modelIndex]);
                LoadNextModel();
            }
        }

        public void SaveModel()
        {
            ModelData test = CreateModelData();

            modelData.Add(test);

            dataManager.MoveFile(importModelPaths[modelIndex], dataManager.modPath.Value + "/" + test.category);

            dataManager.SaveModelData();

            modelIndex++;

            if (modelIndex == importModelPaths.Count - 1)
            {
                BackToMainMenu();
            }
            else
            {
                LoadNextModel();
            }

        }

        public void SkipModel()
        {
            modelIndex++;
            LoadNextModel();
        }

        public void UpdateMeshDropdown()
        {
            meshDropdown.GetComponent<Dropdown>().ClearOptions();

            Dropdown.OptionData newData;

            foreach(Transform t in baseObject.Value.transform.GetChild(0))
            {
                newData = new Dropdown.OptionData();
                newData.text = t.name;
                meshDropdown.GetComponent<Dropdown>().options.Add(newData);
            }

            meshDropdown.GetComponent<Dropdown>().value = 1;
        }

        public void ApplyTexture(RawImage img)
        {
            selectedObject.Value.GetComponent<Outline>().enabled = false;
            selectedObject.Value.GetComponent<MeshRenderer>().materials[selectedSubmesh.Value - 1].SetTexture("_MainTex", img.texture);
            selectedObject.Value.GetComponent<Outline>().enabled = true;

            int i = selectedObject.Value.transform.GetSiblingIndex();
            submeshTextureList[i, selectedSubmesh.Value] = img.name;
        }

        public void UpdateCategoryDropdown()
        {
            categoryDropdown.options.Clear();
            foreach (string s in categories.Items)
            {
                categoryDropdown.options.Add(new Dropdown.OptionData() { text = s });
            }
        }

        public void AddCategory(InputField newCategoryInput)
        {
            string newCategoryName = newCategoryInput.text;

            if (!categories.Items.Contains(newCategoryName))
            {
                categories.Add(newCategoryName);
                dataManager.CreateCategoryDir(newCategoryName);
                UpdateCategoryDropdown();
            }
        }

        public void ToggleCategoryDialog(GameObject window)
        {
            if (window.activeSelf)
            {
                window.SetActive(false);
            }
            else window.SetActive(true);
        }



        public ModelData CreateModelData()
        {
            string importPath = importModelPaths[modelIndex];
            string filename = importPath.Split('\\')[importPath.Split('\\').Length - 1];


            ModelData newModel = new ModelData()
            {
                path = dataManager.modPath.Value,
                submeshTextures = submeshTextureList,
                difficulty = CalculateDifficulty(),
                scale = GetScale(),
                category = categoryDropdown.options[categoryDropdown.value].text
            };

            newModel.path += "/" + newModel.category + "/" + filename;

            return newModel;
        }

        public void InitSubmeshTextureList(GameObject model)
        {
            submeshTextureList = new string[model.transform.GetChild(0).childCount, 12];
        }


        public float CalculateDifficulty()
        {
            float diff = 0;
            float sizeFactor = 2 - GetScale().x;

            int subModels = int.Parse(childMeshes.gameObject.GetComponent<Text>().text);
            int submeshes = int.Parse(totalSubmeshes.gameObject.GetComponent<Text>().text);
            int triangles = int.Parse(totalTriangles.gameObject.GetComponent<Text>().text);

            float triMean = triangles / subModels / 1000;
            float additionalSubmeshes = (subModels - submeshes) / 2;

            diff = subModels + triMean + additionalSubmeshes;
            diff *= sizeFactor;

            return diff;
        }

        public Vector3 GetScale()
        {
            Vector3 scale = Vector3.zero;

            switch (selectedSize.Value)
            {
                case "S":
                    scale = new Vector3(0.5f, 0.5f, 0.5f);
                    break;
                case "M":
                    scale = new Vector3(0.75f, 0.75f, 0.75f);
                    break;
                case "L":
                    scale = new Vector3(1, 1, 1);
                    break;
            }

            return scale;
        }

        public void BackToMainMenu()
        {
            SceneManager.LoadScene("MenuScene");
        }


        public void FillRandomTextures()
        {
            int siblingIndex;
            int materialindex;
            int rand;
            Texture2D texture;

            foreach(Transform t in baseObject.Value.transform.GetChild(0))
            {
                siblingIndex = t.GetSiblingIndex();
                materialindex = 0;

                if (t.gameObject.GetComponent<Outline>() != null)
                {
                    t.gameObject.GetComponent<Outline>().enabled = false;
                }

                foreach (Material m in t.gameObject.GetComponent<MeshRenderer>().materials)
                {
                    rand = Random.Range(0, textures.Items.Count - 1);

                    byte[] bytes = System.IO.File.ReadAllBytes(textures.Items[rand]);
                    texture = new Texture2D(1, 1);
                    texture.LoadImage(bytes);

                    m.SetTexture("_MainTex", texture);

                    submeshTextureList[siblingIndex, materialindex] = textures.Items[rand];
                    materialindex++;
                }

                if (t.gameObject.GetComponent<Outline>() != null)
                {
                    t.gameObject.GetComponent<Outline>().enabled = true;
                }
            }
        }

        private void Start()
        {
            UpdateCategoryDropdown();
        }
    }
}

