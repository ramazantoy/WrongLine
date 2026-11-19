// The player car from Meshy's output: bakes the body and the wheel into game scale and builds them into the car prefab.
//   unity command script run --file .claude/scripts/CarMeshyBuild.cs --entry ClaudeCarMeshy.Run --args '["Assets/Game/Car/Car.prefab"]'
// Inputs, in Assets/Game/Car/Art/MeshyV3/: CarBodyV3.fbx + CarV3_Base.png (the body without wheels, lights painted as flat
// white / red / amber panels) and WheelV3.fbx + WheelV3_Base.png. It writes the baked meshes, the materials, the light mask
// (CarLightMaskBuilder) and rebuilds the Chassis child "CarV3" with the CarView's wheel, steering and body-renderer fields.
// Run it again when a model or its texture changes. The recipe and the Meshy settings: .claude/decisions/Game.md ("Araç v3").
using UnityEditor;
using UnityEngine;

public static class ClaudeCarMeshy
{
    private const string DIR = "Assets/Game/Car/Art/MeshyV3/";
    private const float BodyLength = 4.2f;
    private const float WheelDiameter = 0.66f;
    private const float WheelCenterY = 0.33f;
    private const float WheelHalfTrack = 0.80f;
    private const float WheelAxleZ = 1.25f;
    private const float BodyBottom = 0.18f;

    public static string Run(string prefabPath)
    {
        var sb = new System.Text.StringBuilder();

        SetupTexture(DIR + "CarV3_Base.png", 4096);
        SetupTexture(DIR + "WheelV3_Base.png", 1024);
        Material bodyMat = MakeMaterial(DIR + "CarV3_Body.mat", DIR + "CarV3_Base.png");
        Material wheelMat = MakeMaterial(DIR + "CarV3_Wheel.mat", DIR + "WheelV3_Base.png");

        // Body: +X is the nose in the file, up is +Y; turn it to +Z, scale to 4.2 m long, sit the sill 18 cm above the road.
        Mesh bodySrc = AssetDatabase.LoadAssetAtPath<GameObject>(DIR + "CarBodyV3.fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
        float srcLength = bodySrc.bounds.size.x * 100f;
        float scale = 100f * BodyLength / srcLength;
        // A car is wider than tall: when the file's Y is the longer of the two, the file is Z-up and must be stood up first.
        bool zUp = bodySrc.bounds.size.y > bodySrc.bounds.size.z;
        Quaternion stand = zUp ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;
        sb.Append("zUp=").Append(zUp).Append("\n");
        Matrix4x4 bodyM = Matrix4x4.Rotate(Quaternion.Euler(0f, -90f, 0f) * stand) * Matrix4x4.Scale(Vector3.one * scale);
        Mesh body = Bake(bodySrc, bodyM, DIR + "CarV3_Body.asset", false, BodyBottom);
        Texture2D lightMask = WrongLine.Car.Editor.CarLightMaskBuilder.Build(body, DIR + "CarV3_Base.png", DIR + "CarV3_LightMask.png", 2048);
        bodyMat.SetTexture("_LightMask", lightMask);
        bodyMat.SetFloat("_LightMaskOn", 1f);
        bodyMat.EnableKeyword("_LIGHTMASK_ON");
        bodyMat.SetColor("_EmissionColor", new Color(1.6f, 1.6f, 1.6f, 1f));
        EditorUtility.SetDirty(bodyMat);
        AssetDatabase.SaveAssetIfDirty(bodyMat);
        sb.Append("body size=").Append(body.bounds.size.ToString("F3")).Append(" tris=").Append(body.triangles.Length / 3).Append("\n");

        // Wheel: the axle is Z in the file and the rim face looks outward along +Z. Left faces -X, right faces +X.
        Mesh wheelSrc = AssetDatabase.LoadAssetAtPath<GameObject>(DIR + "WheelV3.fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
        float wScale = WheelDiameter / wheelSrc.bounds.size.x;
        Mesh wheelL = Bake(wheelSrc, Matrix4x4.Rotate(Quaternion.Euler(0f, -90f, 0f)) * Matrix4x4.Scale(Vector3.one * wScale), DIR + "CarV3_WheelL.asset", true, 0f);
        Mesh wheelR = Bake(wheelSrc, Matrix4x4.Rotate(Quaternion.Euler(0f, 90f, 0f)) * Matrix4x4.Scale(Vector3.one * wScale), DIR + "CarV3_WheelR.asset", true, 0f);
        sb.Append("wheel size=").Append(wheelL.bounds.size.ToString("F3")).Append(" tris=").Append(wheelL.triangles.Length / 3).Append("\n");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            if (!AssetDatabase.CopyAsset("Assets/Game/Car/Car.prefab", prefabPath)) return "copy failed";
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform chassis = root.transform.Find("Chassis");
            foreach (string oldName in new[] { "Car_Sport", "CarV3" })
            {
                Transform old = chassis.Find(oldName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            var model = new GameObject("CarV3");
            model.transform.SetParent(chassis, false);
            model.transform.localPosition = new Vector3(0f, -0.4f, 0f);

            Transform bodyNode = Part(model.transform, "Body", body, bodyMat, Vector3.zero);
            Transform fl = Part(model.transform, "Wheel_FL", wheelL, wheelMat, new Vector3(-WheelHalfTrack, WheelCenterY, WheelAxleZ));
            Transform fr = Part(model.transform, "Wheel_FR", wheelR, wheelMat, new Vector3(WheelHalfTrack, WheelCenterY, WheelAxleZ));
            Transform rl = Part(model.transform, "Wheel_RL", wheelL, wheelMat, new Vector3(-WheelHalfTrack, WheelCenterY, -WheelAxleZ));
            Transform rr = Part(model.transform, "Wheel_RR", wheelR, wheelMat, new Vector3(WheelHalfTrack, WheelCenterY, -WheelAxleZ));

            var steering = new GameObject("SteeringWheel");
            steering.transform.SetParent(model.transform, false);
            steering.transform.localPosition = new Vector3(-0.34f, 0.99f, 0.5f);

            var so = new SerializedObject(root.GetComponent<MonoBehaviour>());
            so.FindProperty("_steeringWheel").objectReferenceValue = steering.transform;
            so.FindProperty("_wheelFrontLeft").objectReferenceValue = fl;
            so.FindProperty("_wheelFrontRight").objectReferenceValue = fr;
            so.FindProperty("_wheelRearLeft").objectReferenceValue = rl;
            so.FindProperty("_wheelRearRight").objectReferenceValue = rr;
            so.FindProperty("_bodyRenderer").objectReferenceValue = bodyNode.GetComponent<MeshRenderer>();
            so.FindProperty("_leftLamps").arraySize = 0;
            so.FindProperty("_rightLamps").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (string cube in new[] { "Signal_FL", "Signal_FR", "Signal_RL", "Signal_RR", "SignalLight_L", "SignalLight_R", "TailLight" })
            {
                Transform t = chassis.Find(cube);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            Set(chassis, "Headlight", -0.6f, 0.30f, 2.0f);
            Set(chassis, "HeadlightRight", 0.6f, 0.30f, 2.0f);
            Set(chassis, "HeadlightGlow", 0f, 0.35f, 2.25f);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return sb.ToString();
    }

    private static Transform Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    // centre: move the mesh so its bounds centre is the origin (a wheel turns about its middle).
    // Otherwise the mesh is moved so its lowest point sits at minYTarget (the body keeps its own x/z, centred already).
    private static Mesh Bake(Mesh src, Matrix4x4 m, string path, bool centre, float minYTarget)
    {
        var verts = src.vertices;
        var normals = src.normals;
        var outV = new Vector3[verts.Length];
        var outN = new Vector3[verts.Length];
        float minY = float.MaxValue;
        for (int i = 0; i < verts.Length; i++)
        {
            outV[i] = m.MultiplyPoint3x4(verts[i]);
            outN[i] = normals.Length == verts.Length ? m.MultiplyVector(normals[i]).normalized : Vector3.up;
            if (outV[i].y < minY) minY = outV[i].y;
        }

        if (centre)
        {
            Vector3 lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var v in outV) { lo = Vector3.Min(lo, v); hi = Vector3.Max(hi, v); }
            Vector3 c = (lo + hi) * 0.5f;
            for (int i = 0; i < outV.Length; i++) outV[i] -= c;
        }
        else
        {
            for (int i = 0; i < outV.Length; i++) outV[i].y += minYTarget - minY;
        }

        var mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(path) };
        mesh.indexFormat = verts.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = outV;
        mesh.normals = outN;
        mesh.uv = src.uv;
        mesh.triangles = src.triangles;
        mesh.RecalculateBounds();
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    private static void SetupTexture(string path, int max)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.sRGBTexture = true;
        imp.maxTextureSize = max;
        imp.mipmapEnabled = true;
        imp.SaveAndReimport();
    }

    private static Material MakeMaterial(string path, string texPath)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("WrongLine/CurvedLit")) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetFloat("_Cull", 0f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssetIfDirty(mat);
        return mat;
    }

    private static void Set(Transform parent, string name, float x, float y, float z) { parent.Find(name).localPosition = new Vector3(x, y, z); }
}
