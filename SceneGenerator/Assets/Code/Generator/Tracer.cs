using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

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

        public IntVariable noise;

        public Text pointCount;

        public Text traceTime;

        public bool displayActive;

        private class Point
        {
            public Vector3 coords { get; set; }
            public Color color { get; set; }
            public string type { get; set; }
            public int count { get; set; }
        }

        public void TraceScene()
        {


            DateTime startTime = DateTime.Now;

            Debug.Log("Start Trace Routine. Input is perspectives: " + perspectives + " and resolution:" + resolution);

            dataPoints = new List<Point>();

            Trace(perspectives, resolution, "Default", true);

            Trace(perspectives, 200, "Box", true);

            UpdateTraceStats((DateTime.Now - startTime).Seconds);
        }

        private Color CalcColor(RaycastHit hit, bool shadows)
        {
            Color color = new Color(0, 0, 0);

            switch (hit.collider.gameObject.GetComponent<Label>().label)
            {
                case "Ceiling_0":
                    if(shadows)return Shade(new Color(0, 0, 0.1f, 1), hit);
                    else return new Color(0, 0, 0.5f, 1);
                case "Floor_1":
                    if (shadows) return Shade(new Color(0, 0.1f, 0, 1), hit);
                    else return new Color(0, 0.5f, 0, 1);
                case "Wall_2":
                    if (shadows) return Shade(new Color(0.1f, 0, 0, 1), hit);
                    else return new Color(0.5f, 0, 0, 1);
                default:
                    break;
            }

            Mesh mesh = hit.collider.GetComponent<MeshFilter>().mesh;
            Renderer renderer = hit.collider.GetComponent<MeshRenderer>();
            Texture2D texture2D = null;

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
            if (shadows)
            {
                return Shade(color, hit);
            }
            else
            {
                return color;
            }


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

                if (Physics.Raycast(ray, out shadeHit, 100f, 1 << LayerMask.NameToLayer("Default")))
                {
                    if (shadeHit.collider.gameObject.GetComponent<Label>().label.Equals("Light_3"))
                    {
                        cnt++;
                        dist = shadeHit.distance;
                    }
                }
            }

            float angle = Vector3.Dot(hit.normal, ray.direction);

            if (angle < 0) angle = 0;

            Color directIllum = Vector4.Scale(lightColor, diffuseCoefficient) * (1 - Mathf.Pow(dist / l.range, 2)) * cnt * angle;

            Color shadedColor = color + directIllum;
            
            return ClampColor(shadedColor);
        }

        public Color ClampColor(Color c)
        {
            c.r = Mathf.Clamp(c.r, 0, 1.0f);
            c.g = Mathf.Clamp(c.g, 0, 1.0f);
            c.b = Mathf.Clamp(c.b, 0, 1.0f);

            return c;
        }

        public void Trace(int perspectives, int resolution, string layer, bool shadows)
        {
            Debug.Log("Starting Trace()");

            float step = 1.0f / resolution;

            float x;
            float y;

            Ray ray;

            Point p = new Point();

            for (int k = 0; k < perspectives; k++)
            {
                activeCam = traceCams.Items[k];

                for (int i = 0; i < resolution; i++)
                {
                    for (int j = 0; j < resolution; j++)
                    {
                        x = i * step + UnityEngine.Random.Range(-step * noise.Value, step * noise.Value);
                        y = j * step + UnityEngine.Random.Range(-step * noise.Value, step * noise.Value);

                        ray = activeCam.ViewportPointToRay(new Vector3(x, y, 0));

                        //Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 100000f);

                        try
                        {
                            foreach (RaycastHit h in Physics.RaycastAll(ray, 100f, 1 << LayerMask.NameToLayer(layer)))
                            {
                                Vector3 coords = new Vector3(h.point.x + 500, h.point.y, h.point.z - 500);

                                dataPoints.Add(new Point()
                                {
                                    coords = coords,
                                    type = h.collider.GetComponent<Label>().label,
                                    color = CalcColor(h, shadows),
                                    count = h.collider.gameObject.GetComponent<Label>().counter
                                });
                            }
                        }
                        catch(Exception)
                        {
                        }
                        
                    }
                }
            }
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

            StringBuilder outputForAI = new StringBuilder("X;Y;Z;R;G;B;Label;Count");

            foreach(Point p in dataPoints)
            {
                outputForAI.Append('\n').Append(p.coords.x.ToString().Replace(",", ".")).Append(';')
                    .Append(p.coords.y.ToString().Replace(",", ".")).Append(';')
                    .Append(p.coords.z.ToString().Replace(",", ".")).Append(';')
                    .Append(p.color.r.ToString().Replace(",", ".")).Append(';')
                    .Append(p.color.g.ToString().Replace(",", ".")).Append(';')
                    .Append(p.color.b.ToString().Replace(",", ".")).Append(';')
                    .Append(p.type.Split('_')[1]).Append(';').Append(p.count.ToString());
            }

            StringBuilder outputForDisplay = new StringBuilder("");

            foreach (Point p in dataPoints)
            {
                outputForDisplay.Append(p.coords.x.ToString().Replace(",", ".")).Append(';')
                    .Append(p.coords.y.ToString().Replace(",", ".")).Append(';')
                    .Append(p.coords.z.ToString().Replace(",", ".")).Append(';')
                    .Append(p.color.r.ToString().Replace(",", ".")).Append(';')
                    .Append(p.color.g.ToString().Replace(",", ".")).Append(';')
                    .Append(p.color.b.ToString().Replace(",", ".")).Append('\n');
            }

            //dataManager.WriteToCSV(outputForAI.ToString(), sceneGen.usedDiff, "xyzrgblc", dataPoints.Count, sceneGen.loadedModels.Count);

            dataManager.WriteToCSV(outputForDisplay.ToString(), sceneGen.usedDiff, "xyzrgb", dataPoints.Count, sceneGen.loadedModels.Count);
        }

        public void UpdateTraceStats(double timeToTrace)
        {
            pointCount.text = dataPoints.Count.ToString();
            traceTime.text = timeToTrace.ToString() + "s";
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

            GameObject.Find("PlaneDownCam").GetComponent<Label>().label = "Ceiling_0";
            GameObject.Find("PlaneUpCam").GetComponent<Label>().label = "Floor_1";
            GameObject.Find("PlaneFrontCam").GetComponent<Label>().label = "Wall_2";
            GameObject.Find("PlaneBackCam").GetComponent<Label>().label = "Wall_2";
            GameObject.Find("PlaneRightCam").GetComponent<Label>().label = "Wall_2";
            GameObject.Find("PlaneLeftCam").GetComponent<Label>().label = "Wall_2";
            GameObject.Find("LightDummy").GetComponent<Label>().label = "Light_3";
            GameObject.Find("PlaneDownCam").GetComponent<Label>().counter = 1;
            GameObject.Find("PlaneUpCam").GetComponent<Label>().counter = 1;
            GameObject.Find("PlaneFrontCam").GetComponent<Label>().counter = 1;
            GameObject.Find("PlaneBackCam").GetComponent<Label>().counter = 2;
            GameObject.Find("PlaneRightCam").GetComponent<Label>().counter = 3;
            GameObject.Find("PlaneLeftCam").GetComponent<Label>().counter = 4;
            GameObject.Find("LightDummy").GetComponent<Label>().counter = 1;

        }
    }

}