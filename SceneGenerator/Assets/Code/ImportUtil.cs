using SimpleFileBrowser;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneGenerator
{
    public class ImportUtil : MonoBehaviour
    {
        public DataUtil data;

        public int currentIndex;

        public Camera importSceneCam;

        public string importDirectoryPath;

        IList<string> textureDataTypes = new List<string> { "psd", "tiff", "jpg", "tga", "png", "gif", "bmp", "iff", "pict" };

        IList<string> modelDataTypes = new List<string> { "obj", "fbx", "max",
            "c4d", "blend", "ma", "mb",
            "3ds", "ply", "dae", "xml", "stl", "lwo", "lxo", "lws" };

        public void loadImportScene()
        {
            //Change Scene
            SceneManager.LoadScene("ImportScene");
            //Choose Directory
            ChooseDirectory();
        }


        void Start()
		{
           // ChooseDirectory();
        }

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
                Debug.Log(paths[0]);
                importDirectoryPath = FileBrowser.Result[0];
                Debug.Log(importDirectoryPath);
                ParseDirectory();
            },
                                      () => { Debug.Log("Canceled"); },
                                      FileBrowser.PickMode.Folders, false, null, null, "Select Folder", "Select");
        }




            public void ParseDirectory()
        {
            foreach(string file in Directory.GetFiles(importDirectoryPath))
            {
                string fileType = file.Split('.')[file.Split('.').Length - 1];

                if (textureDataTypes.Contains(fileType))
                {
                    data.MoveFile(file, data.texPath);
                }else if (modelDataTypes.Contains(fileType))
                {
                    data.models.Add(file);
                }
            }
            //startModelImport();
        }

        public void LoadModel()
        {

        }

        public void ModelImport()
        {

        }

	}
}



