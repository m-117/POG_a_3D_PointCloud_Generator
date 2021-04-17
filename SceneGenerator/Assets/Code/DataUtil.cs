using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;


namespace SceneGenerator
{

    public class DataUtil : MonoBehaviour
    {
        private static DataUtil _Instance;
        public static DataUtil Instance
        {
            get
            {
                if (!_Instance)
                {
                    _Instance = new GameObject().AddComponent<DataUtil>();
                    _Instance.name = _Instance.GetType().ToString();

                    DontDestroyOnLoad(_Instance.gameObject);
                }
                return _Instance;
            }
        }


        public IList<string> textures;
        public IList<string> models;
        public IList<ModelData> modelData;
        public string texPath;
        public string modPath;

        public class ModelData
        {
            public string path { get; set; }
            public IList<string> submeshTextures { get; set; }
            public int difficulty { get; set; }
            public Vector3 scale { get; set; }
        }

        public void LoadModelData()
        {
            if (File.Exists(modPath + "/modelData.txt"))
            {
                string modJson = File.ReadAllText(modPath + "/modelData.txt");

                models = JsonConvert.DeserializeObject<List<string>>(modJson);

                try
                {
                    foreach (String s in models)
                    {
                        modelData.Add(JsonConvert.DeserializeObject<ModelData>(s));
                    }
                }
                catch (Exception)
                {
                    models = new List<string>();
                }
                
            }
            else WriteJsonToFile("", modPath + "/modelData.txt");

        }

        public void LoadTextureData()
        {
            foreach(string file in Directory.GetFiles(texPath))
            {
                textures.Add(file);
            }
        }

        public void SaveModelData()
        {
            models = new List<string>();

            foreach (ModelData d in modelData)
            {
                models.Add(JsonConvert.SerializeObject(d, Formatting.Indented));
            }

            string modJson = JsonConvert.SerializeObject(models, Formatting.Indented);

            WriteJsonToFile(modJson, modPath + "/modelData.txt");
        }

        public void WriteJsonToFile(string json, string filepath)
        {
            StreamWriter file;

            try
            {
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }

                file = File.CreateText(filepath);
                File.WriteAllText(filepath, json);
                file.Close();

                //if (!File.Exists(filepath))
                //{
                //    file = File.CreateText(filepath);
                //    file.WriteLine(json);
                //    file.Close();
                //}
                //else
                //{
                //    File.Delete(filepath);
                //    file = File.CreateText(filepath);
                //    File.WriteAllText(filepath, json);
                //}

            }
            catch (Exception e)
            {
                if (!Directory.Exists(modPath)) Directory.CreateDirectory(modPath);                
                Debug.Log(e.Message);
            }
        }

        public void MoveFile(string sourcePath, string directory)
        {
            //TODO: Support for Unix-Systems (check OS + split 

            string fileName = sourcePath.Split('\\')[sourcePath.Split('\\').Length - 1];

            if(!File.Exists(directory + "/" + fileName))
            {
                File.Move(sourcePath, directory + "/" + fileName);
                if (directory.Equals(texPath))
                {
                    textures.Add(directory + "/" + fileName);
                }
            }       
        }


        void Start()
        {
            texPath = Application.persistentDataPath + "/textures";
            modPath = Application.persistentDataPath + "/models";
            if (!Directory.Exists(modPath)) Directory.CreateDirectory(modPath);
            if (!Directory.Exists(texPath)) Directory.CreateDirectory(texPath);

            Debug.Log(texPath);

            modelData = new List<ModelData>();
            models = new List<string>();
            textures = new List<string>();

            LoadTextureData();

            LoadModelData();
        }
    }
}
