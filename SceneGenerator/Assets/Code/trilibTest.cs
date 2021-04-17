using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TriLib;
using UnityEngine;

public class trilibTest : MonoBehaviour
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

    public bool TryLoadObject(string filename, out GameObject model)
    {
        model = null;
        if (string.IsNullOrEmpty(filename)) return false;
        if (!File.Exists(filename)) return false;
        var assetLoaderOptions = GetAssetLoaderOptions();
        using (var assetLoader = new AssetLoader())
        {
            try
            {
                model = assetLoader.LoadFromFile(filename, assetLoaderOptions);

                //model = assetLoader.LoadFromFileWithTextures(filename, assetLoaderOptions);
                if (assetLoader.MeshData == null || assetLoader.MeshData.Length == 0) return false;
            }
            catch (Exception e)
            {
                if (model != null)
                {
                    Destroy(model);
                }
                Debug.Log("error");
                Debug.Log(e.Message);
                return false;
            }
        }

        foreach (Camera camera in model.GetComponentsInChildren<Camera>())
        {
            Destroy(camera);
        }

        return true;
    }


    public void testRoutine()
    {
        GameObject testObj;
        
        if(TryLoadObject(Application.dataPath + "/Objects/test.zip", out testObj))
        {
            testObj.transform.position = new Vector3(5, 5, 5);
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        GameObject testObj = new GameObject();

        if (TryLoadObject("C:\\Users\\marco\\Desktop\\Thesis\\models\\Table.obj", out testObj))
        {
            Debug.Log("loaded");
            testObj.transform.position = new Vector3(0, 0, 0);

            Debug.Log(testObj.GetComponentsInChildren<MeshRenderer>()[0].material);
            
            //foreach (Transform child in testObj.transform)
            //{
                foreach(Transform child in testObj.transform.GetChild(0))
                {
                //child2.GetComponent<MeshRenderer>().material = Resources.Load("Material/" + child2.name + "Mat",
                //    typeof(Material)) as Material;
                child.gameObject.AddComponent<MeshCollider>();

                child.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", Resources.Load("wood2") as Texture2D);

                    if (child.name.Equals("Box002"))
                    {
                        child.name = "wood";
                        child.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", Resources.Load(child.name) as Texture2D);
                    }

                    Debug.Log(child.name);
                //}
                Debug.Log(child.name);
            }


        }

        //using (var assetLoader = new AssetLoader())
        //{
        //    var assetLoaderOptions = AssetLoaderOptions.CreateInstance();   //Creates the AssetLoaderOptions instance.
        //                                                                    //AssetLoaderOptions let you specify options to load your model.
        //                                                                    //(Optional) You can skip this object creation and it's parameter or pass null.

        //    //You can modify assetLoaderOptions before passing it to LoadFromFile method. You can check the AssetLoaderOptions API reference at:
        //    //https://ricardoreis.net/trilib/manual/html/class_tri_lib_1_1_asset_loader_options.html

        //    var wrapperGameObject = gameObject;                             //Sets the game object where your model will be loaded into.
        //                                                                    //(Optional) You can skip this object creation and it's parameter or pass null.
        //    var myGameObject = assetLoader.LoadFromFileWithTextures(Application.dataPath + "/Objects/table.zip", assetLoaderOptions, wrapperGameObject); //Loads the model synchronously and stores the reference in myGameObject.

        //    myGameObject.transform.position = new Vector3(0, 0, 0);

        //    myGameObject.GetComponentsInChildren<MeshRenderer>()[0].material = 

        //    Debug.Log(myGameObject.GetComponentsInChildren<MeshRenderer>()[0].material);
        //}

    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void testasync()
    {
        using (var assetLoaderAsync = new AssetLoaderAsync())
        {
            var assetLoaderOptions = AssetLoaderOptions.CreateInstance();   //Creates the AssetLoaderOptions instance.
                                                                            //AssetLoaderOptions let you specify options to load your model.
                                                                            //(Optional) You can skip this object creation and it's parameter or pass null.

            //You can modify assetLoaderOptions before passing it to LoadFromFile method. You can check the AssetLoaderOptions API reference at:
            //https://ricardoreis.net/trilib/manual/html/class_tri_lib_1_1_asset_loader_options.html

            var wrapperGameObject = gameObject;                             //Sets the game object where your model will be loaded into.
                                                                            //(Optional) You can skip this object creation and it's parameter or pass null.

            var thread = assetLoaderAsync.LoadFromFile("PATH TO MY FILE.FBX", assetLoaderOptions, wrapperGameObject, delegate (GameObject myGameObject) {
                //Here you can get the reference to the loaded model using myGameObject.
            }); //Loads the model asynchronously and returns the reference to the created Task/Thread.
        }
    }

}
