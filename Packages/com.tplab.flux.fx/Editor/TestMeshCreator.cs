using UnityEditor;
using UnityEngine;

public static class TestMeshCreator
{
    [MenuItem("Tools/FluxFX/Create Test Meshes")]
    static void CreateTestMeshes()
    {
        CreatePrimitiveMesh(PrimitiveType.Cube, "Assets/Cube.asset");
        CreatePrimitiveMesh(PrimitiveType.Quad, "Assets/Quad.asset");
        CreatePrimitiveMesh(PrimitiveType.Sphere, "Assets/Sphere.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void CreatePrimitiveMesh(PrimitiveType type, string path)
    {
        var gameObject = GameObject.CreatePrimitive(type);
        var sourceMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;

        var mesh = Object.Instantiate(sourceMesh);
        mesh.name = type.ToString();

        AssetDatabase.CreateAsset(mesh, path);
        Object.DestroyImmediate(gameObject);
    }
}