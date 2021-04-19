using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SceneGenerator
{
    public class Tracer : MonoBehaviour
    {
        public int resolution;

        public int perspectives;

        public Generator sceneGen;

        public DataManager dataManager;

        public CameraRuntimeSet traceCams;

        public Camera activeCam;

        private IList<Point> dataPoints;

        public Vector3 ambientCoefficient = new Vector3(0.1f, 0.1f, 0.1f);

        public bool displayActive;

        private class Point
        {
            public Vector3 coords { get; set; }
            public Color color { get; set; }
            public string type { get; set; }
        }

        public void TraceScene()
        {
            Debug.Log("Start Trace Routine. Input is perspectives: " + perspectives + " and resolution:" + resolution);

            sceneGen.DestroyRigidbody();

            dataPoints = new List<Point>();

            //int layerMaskModels = 1 << 0;
            //layerMaskModels = ~layerMaskModels;
            //int layerMaskBox = 1 << 8;
            //layerMaskBox = ~layerMaskBox;

            Trace(perspectives, resolution, "Default");

            Trace(perspectives, 50, "Box");

        }

        private Color CalcColor(RaycastHit hit)
        {
            Mesh mesh = hit.collider.GetComponent<MeshFilter>().mesh;
            Renderer renderer = hit.collider.GetComponent<MeshRenderer>();
            Texture2D texture2D = null;
            Color color = new Color(0, 0, 0);
            Vector2 tiling;
            Vector2 pCoord = hit.textureCoord;

            if (mesh.subMeshCount == 1)
            {
                texture2D = renderer.material.mainTexture as Texture2D;

                if (texture2D == null) color = renderer.material.GetColor("_Color");
            }
            else
            {
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    if (hit.triangleIndex > mesh.GetIndexStart(i) / 3)
                    {
                        texture2D = renderer.materials[i].GetTexture("_MainTex") as Texture2D;

                        if (texture2D == null) color = renderer.materials[i].GetColor("_Color");
                    }
                }
            }
            if (texture2D != null)
            {
                pCoord.x *= texture2D.width;
                pCoord.y *= texture2D.height;

                tiling = renderer.material.mainTextureScale;
                color = texture2D.GetPixel(Mathf.FloorToInt(pCoord.x * tiling.x), Mathf.FloorToInt(pCoord.y * tiling.y));
            }

            return Shade(color, hit);

            //return color;
        }

        private Color Shade(Color color, RaycastHit hit)
        {
            Light l = GameObject.Find("SceneLight").GetComponent<Light>();
            Vector3 lightPos = l.transform.position;
            Color lightColor = l.color;

            int cnt = 0;
            int lightSamples = 10;
            float dist = 0;
            Color diffuseCoefficient = new Color(1f / lightSamples, 1f / lightSamples, 1f / lightSamples, 1);

            Ray ray = new Ray(hit.point, Vector3.zero);
            RaycastHit shadeHit;

            for (int i = 0; i < lightSamples; i++)
            {

                Vector3 sample = new Vector3(lightPos.x + UnityEngine.Random.Range(-0.25f, 0.25f),
                    1.5f, lightPos.z + UnityEngine.Random.Range(-0.25f, 0.25f));

                ray.direction = sample - hit.point;

                if (Physics.Raycast(ray, out shadeHit, 100, 0))
                {
                    if (shadeHit.collider.CompareTag("light"))
                    {
                        cnt++;
                        dist = shadeHit.distance;
                    }
                }
            }

            float angle = Vector3.Dot(hit.normal, ray.direction);

            if (angle < 0) angle = 0;

            Color directIllum = Vector4.Scale(lightColor, diffuseCoefficient) * (1 - Mathf.Pow(dist / l.range, 2)) * cnt / 2 * angle;

            Color shadedColor = color + directIllum;

            return shadedColor;
        }

        public void Trace(int perspectives, int resolution, string layer)
        {
            Debug.Log("Starting Trace()");

            float step = 1.0f / resolution;

            Ray ray;

            Point p = new Point();

            for (int k = 0; k < perspectives; k++)
            {
                activeCam = traceCams.Items[k];

                Debug.Log("Entered Loop. ActiveCam : " + activeCam.name);

                for (int i = 0; i < resolution; i++)
                {
                    for (int j = 0; j < resolution; j++)
                    {
                        ray = activeCam.ViewportPointToRay(new Vector3(i * step, j * step, 0));
                        // Raycast mit Verdeckung (only first hit)
                        //
                        //if(Physics.Raycast(ray, out hit, 10))
                        //{
                        //    p.coords = hit.point;
                        //    p.type = hit.collider.tag;
                        //    p.color = calcColor(hit);
                        //    //save p
                        //    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        //    sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                        //    sphere.transform.position = p.coords;
                        //    sphere.GetComponent<Renderer>().material.SetColor("_Color", p.color);
                        //    sphere.tag = p.type;
                        //    sphere.GetComponent<Collider>().enabled = false;
                        //}


                        // Durchdringender Raycast
                        //
                        try
                        {
                            foreach (RaycastHit h in Physics.RaycastAll(ray, 100f, 1 << LayerMask.NameToLayer(layer)))
                            {
                                dataPoints.Add(new Point()
                                {
                                    coords = h.point,
                                    type = h.collider.gameObject.GetComponent<Label>().label,
                                    color = CalcColor(h)
                                });
                                //p.coords = h.point;
                                //p.type = h.collider.gameObject.GetComponent<Label>().label;
                                //p.color = CalcColor(h);
                                //dataPoints.Add(p);
                                //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                //sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                                //sphere.transform.position = p.coords;
                                //sphere.GetComponent<Renderer>().material.SetColor("_Color", p.color);
                                //sphere.tag = p.type;
                                //sphere.GetComponent<Collider>().enabled = false;
                            }
                        }
                        catch(Exception)
                        {
                        }
                        
                    }
                }
            }

            Debug.Log("Points:" + dataPoints.Count);

            //foreach (Point s in dataPoints)
            //{
            //    Debug.Log("Point at:" + s.coords);
            //    Debug.Log("Point Color:" + s.color);
            //    Debug.Log("Point LabeL:" + s.type);
            //}
        }

        public void DisplayDatapoints()
        {
            if (displayActive)
            {
                Destroy(GameObject.Find("displayParent"));
                displayActive = false;
            }
            else
            {
                GameObject parent = new GameObject();
                parent.name = "displayParent";

                foreach (Point p in dataPoints)
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.parent = parent.transform;
                    sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    sphere.transform.position = p.coords;
                    sphere.GetComponent<Renderer>().material.SetColor("_Color", p.color);
                    sphere.AddComponent<Label>().label = p.type;
                    sphere.GetComponent<Collider>().enabled = false;
                    sphere.name = "Sphere";
                }
                displayActive = true;
            }
        }


        public void ExportDataToCSV()
        {

            StringBuilder outputForAI = new StringBuilder("X;Y;Z;R;G;B;Label");

            foreach(Point p in dataPoints)
            {
                outputForAI.Append('\n').Append(p.coords.x).Append(';')
                    .Append(p.coords.y).Append(';')
                    .Append(p.coords.z).Append(';')
                    .Append(p.color.r).Append(';')
                    .Append(p.color.g).Append(';')
                    .Append(p.color.b).Append(';')
                    .Append(p.type);
            }

            StringBuilder outputForDisplay = new StringBuilder("");

            foreach (Point p in dataPoints)
            {
                outputForDisplay.Append(p.coords.x).Append(';')
                    .Append(p.coords.y).Append(';')
                    .Append(p.coords.z).Append(';')
                    .Append(p.color.r).Append(';')
                    .Append(p.color.g).Append(';')
                    .Append(p.color.b).Append('\n');
            }

            //dataManager.WriteToCSV(outputForAI.ToString());

            dataManager.WriteToCSV(outputForDisplay.ToString());

            //List<string[]> rowData = new List<string[]>();

            //string[] rowDataTemp = new string[7];
            //rowDataTemp[0] = "X";
            //rowDataTemp[1] = "Y";
            //rowDataTemp[2] = "Z";
            //rowDataTemp[3] = "R";
            //rowDataTemp[4] = "G";
            //rowDataTemp[5] = "B";
            //rowDataTemp[6] = "Label";
            //rowData.Add(rowDataTemp);

            //// You can add up the values in as many cells as you want.
            //for (int i = 0; i < 10; i++)
            //{
            //    rowDataTemp = new string[3];
            //    rowDataTemp[0] = "Sushanta" + i; // name
            //    rowDataTemp[1] = "" + i; // ID
            //    rowDataTemp[2] = "$" + UnityEngine.Random.Range(5000, 10000); // Income
            //    rowData.Add(rowDataTemp);
            //}

            //string[][] output = new string[rowData.Count][];

            //for (int i = 0; i < output.Length; i++)
            //{
            //    output[i] = rowData[i];
            //}

            //int length = output.GetLength(0);
            //string delimiter = ",";

            //StringBuilder sb = new StringBuilder();

            //for (int index = 0; index < length; index++)
            //    sb.AppendLine(string.Join(delimiter, output[index]));

            ////DataManager.WriteCSV(sb);

            //string filePath = getPath();

            //StreamWriter outStream = System.IO.File.CreateText(filePath);
            //outStream.WriteLine(sb);
            //outStream.Close();
        }

        private void Start()
        {
            traceCams.Items.Clear();
            traceCams.Add(GameObject.Find("TraceCamFront").GetComponent<Camera>());
            traceCams.Add(GameObject.Find("TraceCamRight").GetComponent<Camera>());
            traceCams.Add(GameObject.Find("TraceCamDown").GetComponent<Camera>());
            traceCams.Add(GameObject.Find("TraceCamBack").GetComponent<Camera>());
            traceCams.Add(GameObject.Find("TraceCamLeft").GetComponent<Camera>());
            traceCams.Add(GameObject.Find("TraceCamUp").GetComponent<Camera>());

            GameObject.Find("PlaneDownCam").GetComponent<Label>().label = "Ceiling";
            GameObject.Find("PlaneUpCam").GetComponent<Label>().label = "Floor";
            GameObject.Find("PlaneFrontCam").GetComponent<Label>().label = "Wall";
            GameObject.Find("PlaneBackCam").GetComponent<Label>().label = "Wall";
            GameObject.Find("PlaneRightCam").GetComponent<Label>().label = "Wall";
            GameObject.Find("PlaneLeftCam").GetComponent<Label>().label = "Wall";
            GameObject.Find("LightDummy").AddComponent<Label>().label = "Light";

        }
    }

}