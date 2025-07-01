using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TerrainCreator : MonoBehaviour
{
    GraphicsBuffer vertexBuffer;
    GraphicsBuffer triangleBuffer;
    Mesh mesh;
    public ComputeShader computeShader;
    public int vertexCount = 16;
    public int terrainSize = 10;
    public Material material;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mesh = GenerateMesh(vertexCount, vertexCount, terrainSize, terrainSize);
        vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.vertexCount, sizeof(float) * 3);
        vertexBuffer.SetData(mesh.vertices);

        triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.triangles.Length, sizeof(int));
        triangleBuffer.SetData(mesh.triangles);

        // Configure the Compute Shader
        computeShader.SetBuffer(0, "mesh", vertexBuffer);
    }

    // Update is called once per frame
    void Update()
    {
        computeShader.SetFloat("time", Time.time);
        computeShader.Dispatch(0, vertexCount * vertexCount / 16, 1, 1);

        RenderParams rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000*Vector3.one); // use tighter bounds
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetBuffer("_Positions", vertexBuffer);
        rp.matProps.SetInt("_BaseVertexIndex", (int)mesh.GetBaseVertex(0));
        rp.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(new Vector3(0, 0, 0)));

        Graphics.RenderPrimitivesIndexed(rp,
            MeshTopology.Triangles,
            triangleBuffer,
            triangleBuffer.count,
            (int)mesh.GetIndexStart(0),
            1);
    }

    void OnDestroy()
    {
        vertexBuffer?.Dispose();
        triangleBuffer?.Dispose();
    }

    Mesh GenerateMesh(int numVerticesX = 16, int numVerticesZ = 16, float planeSizeX = 10f, float planeSizeZ = 10f)
    {

        Mesh mesh = new Mesh();
        mesh.name = "SubdividedPlane";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();


        // Generate vertices and UVs
        for (int z = 0; z < numVerticesZ; z++)
        {
            for (int x = 0; x < numVerticesX; x++)
            {
                float u = (float)x / (numVerticesX - 1);
                float v = (float)z / (numVerticesZ - 1);
                vertices.Add(new Vector3((u - 0.5f) * planeSizeX, 0, (v - 0.5f) * planeSizeZ));
                uvs.Add(new Vector2(u, v));
                normals.Add(Vector3.up);
            }
        }

        // Generate triangles
        for (int z = 0; z < numVerticesZ - 1; z++)
        {
            for (int x = 0; x < numVerticesX - 1; x++)
            {
                int topLeft = z * numVerticesX + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * numVerticesX + x;
                int bottomRight = bottomLeft + 1;

                // First triangle
                triangles.Add(topLeft);
                triangles.Add(bottomLeft);
                triangles.Add(topRight);

                // Second triangle
                triangles.Add(topRight);
                triangles.Add(bottomLeft);
                triangles.Add(bottomRight);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.normals = normals.ToArray();


        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }
}
