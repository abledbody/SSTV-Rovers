using System;
using System.Linq;

using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class VectorMesh : MonoBehaviour {
    public Vector3[] vertices;
    public Edge[] edges = new Edge[0];

    public void OnRenderObject() {
        var cam = SceneView.currentDrawingSceneView?.camera ?? Camera.current ?? Camera.main;
        if (cam == null) return;

        // Prints in binary.
        //print(Convert.ToString(cam.cullingMask, 2));
        // Don't draw if the layer is culled
        if ((cam.cullingMask & (1 << gameObject.layer)) == 0) return;

        GL.PushMatrix();
        GL.LoadProjectionMatrix(cam.projectionMatrix);
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Color(Color.white);
        // Use a blank material so that the lines are not affected by lighting
        var mat = new Material(Shader.Find("Hidden/Internal-Colored"));
        mat.SetPass(0);
        GL.Begin(GL.LINES);

        for (int i = 0; i < edges.Length; i++) {
            var edge = edges[i];
            Vector3 start, end;
            try {
                start = vertices[edge.start];
            } catch (IndexOutOfRangeException) {
                edges = edges.Where(x => x.start != edge.start && x.end != edge.start).ToArray();
                continue;
            }
            try {
                end = vertices[edge.end];
            } catch (IndexOutOfRangeException) {
                edges = edges.Where(x => x.start != edge.end && x.end != edge.end).ToArray();
                continue;
            }
            GL.Vertex(start);
            GL.Vertex(end);
        }

        GL.End();
        GL.PopMatrix();
    }
}

[Serializable]
public struct Edge {
    public int start;
    public int end;
}