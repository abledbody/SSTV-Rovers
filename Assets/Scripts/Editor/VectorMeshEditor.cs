using System.Linq;

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VectorMesh))]
public class VectorMeshEditor : Editor {
	const float NEW_BUTTON_OFFSET = 0.2f;
	const float BUTTON_SIZE = 0.1f;

	private VectorMesh mesh;
	private int selectedVertex = -1;
	private int selectedVertex2 = -1;

	private void OnEnable() {
		mesh = (VectorMesh)target;
	}

	private void OnSceneGUI() {
		if (selectedVertex >= mesh.vertices.Length) selectedVertex = -1;
		if (selectedVertex2 >= mesh.vertices.Length) selectedVertex2 = -1;

		Vector3 vertexPos;
		float handleSize;

		// Draw edges
		for (int i = 0; i < mesh.edges.Length; i++) {
			var edge = mesh.edges[i];
			(int start, int end) = (edge.start, edge.end);
			Vector3 startPos = mesh.vertices[start];
			Vector3 endPos = mesh.vertices[end];
			Handles.color = Color.magenta;
			Handles.DrawLine(startPos, endPos);
		}

		// Draw vertices
		for (int i = 0; i < mesh.vertices.Length; i++) {
			vertexPos = mesh.vertices[i];
			handleSize = HandleUtility.GetHandleSize(vertexPos) * 0.04f;

			if (i == selectedVertex)
				Handles.color = Color.blue;
			else if (i == selectedVertex2)
				Handles.color = Color.blue;
			else
				Handles.color = Color.magenta;

			if (Handles.Button(vertexPos, Quaternion.identity, handleSize, handleSize, Handles.DotHandleCap)) {
				if (Event.current.shift)
					selectedVertex2 = i;
				else {
					selectedVertex = i;
					selectedVertex2 = -1;
				}
			}

			if (selectedVertex == i) {
				Vector3 newPosition = Handles.PositionHandle(vertexPos, Quaternion.identity);
				if (vertexPos != newPosition) {
					Undo.RecordObject(mesh, "Move vertex");
					mesh.vertices[i] = newPosition;
				}
			}
		}

		if (selectedVertex == -1) return;

		vertexPos = mesh.vertices[selectedVertex];
		handleSize = HandleUtility.GetHandleSize(vertexPos);
		var addVertexButtonPos = vertexPos + (-Camera.current.transform.right + Camera.current.transform.up) * NEW_BUTTON_OFFSET * handleSize;
		var removeVertexButtonPos = vertexPos + (Camera.current.transform.right + Camera.current.transform.up) * NEW_BUTTON_OFFSET * handleSize;

		Handles.color = Color.green;

		// Draw a button to add a vertex
		if (Handles.Button(addVertexButtonPos, Quaternion.identity, handleSize * BUTTON_SIZE, handleSize * BUTTON_SIZE, Handles.DotHandleCap)) {
			Undo.RecordObject(mesh, "Add vertex");
			mesh.vertices = mesh.vertices.Append(vertexPos).ToArray();
			selectedVertex = mesh.vertices.Length - 1;
		}

		Handles.color = Color.red;

		// Draw a button to remove a vertex
		if (Handles.Button(removeVertexButtonPos, Quaternion.identity, handleSize * BUTTON_SIZE, handleSize * BUTTON_SIZE, Handles.DotHandleCap)) {
			Undo.RecordObject(mesh, "Remove vertex");
			mesh.vertices = mesh.vertices.Where((x, index) => index != selectedVertex).ToArray();
			mesh.edges = mesh.edges.Where(x => x.start != selectedVertex && x.end != selectedVertex).ToArray();
			selectedVertex = -1;
		}
		
		if (selectedVertex2 == -1) return;
		
		var vertexPos2 = mesh.vertices[selectedVertex2];
		var connected = mesh.edges.Any(x => (x.start == selectedVertex && x.end == selectedVertex2) || (x.start == selectedVertex2 && x.end == selectedVertex));

		// Put the edge button in the middle of the two vertices
		var edgeButtonPos = (vertexPos + vertexPos2) / 2;
		handleSize = HandleUtility.GetHandleSize(edgeButtonPos);

		Handles.color = connected ? Color.red : Color.green;

		// Draw a button to connect or disconnect the two vertices
		if (Handles.Button(edgeButtonPos, Quaternion.identity, handleSize * BUTTON_SIZE, handleSize * BUTTON_SIZE, Handles.DotHandleCap)) {
			if (connected) {
				Undo.RecordObject(mesh, "Disconnect vertices");
				mesh.edges = mesh.edges.Where(x => 
					x.start != selectedVertex && x.end != selectedVertex &&
					x.start != selectedVertex2 && x.end != selectedVertex2
				).ToArray();
			}
			else {
				Undo.RecordObject(mesh, "Connect vertices");
				var edge = new Edge { start = selectedVertex, end = selectedVertex2 };
				ArrayUtility.Add(ref mesh.edges, edge);
			}
		}
	}
}