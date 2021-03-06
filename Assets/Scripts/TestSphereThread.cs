using UnityEngine;
using System.Collections;
using nobnak.Geometry;
using System.Collections.Generic;
using System.Text;
using System.Threading;

public class TestSphereThread : MonoBehaviour {
	public TestIsosphere isoSphere;
	public float bulkReduction = 0.5f;
	
	private Simplification _simp;
	private Mesh _sphere;
	private bool _reductionInProgress = false;
	
	private Vector3[] _outVertices;
	private int[] _outTriangles;	

	void Start () {
		_sphere = isoSphere.GetComponent<MeshFilter>().mesh;
		_simp = new Simplification(_sphere.vertices, _sphere.triangles);
		
		StartCoroutine("Collapse");
	}
	
	void OnGUI() {
		GUILayout.BeginHorizontal();
		var buf = new StringBuilder();
		buf.AppendFormat("Vertex:{0}\n", _sphere.vertexCount);
		buf.AppendFormat("Triangle:{0}", _sphere.triangles.Length / 3);
		GUILayout.TextField(buf.ToString());
		GUILayout.EndHorizontal();
	}
	
	IEnumerator Collapse() {
		while (enabled) {
			yield return 0;
			
			lock (this) {
				if (_reductionInProgress)
					continue;
				
				if (_sphere.vertexCount < 10) {
					UpdateSphere();
					yield return new WaitForSeconds(2f);
					isoSphere.Reset();
					_sphere = isoSphere.GetComponent<MeshFilter>().mesh;
					_simp = new Simplification(_sphere.vertices, _sphere.triangles);
					_outVertices = null;
				}
				
				_reductionInProgress = true;
				if (_outVertices != null)
					UpdateSphere();
				var targetEdgeCount = (int)(bulkReduction * _simp.costs.Count);
				ThreadPool.QueueUserWorkItem(new WaitCallback(Reduction), targetEdgeCount);
			}
		}
	}
	
	void Reduction(System.Object targetEdgeCountObj) {
		try {
			var targetEdgeCount = (int) targetEdgeCountObj;
			while (targetEdgeCount < _simp.costs.Count) {
				CollapseAnEdge();
			}
			_simp.ToMesh(out _outVertices, out _outTriangles);
		}finally {
			lock(this) {
				_reductionInProgress = false;
			}
		}
	}
	
	void CollapseAnEdge () {
		var edgeCost = _simp.costs.RemoveFront();
		_simp.CollapseEdge(edgeCost);
	}
	
	void UpdateSphere() {
		_sphere.Clear();
		_sphere.vertices = _outVertices;
		_sphere.triangles = _outTriangles;
		_sphere.RecalculateNormals();
		_sphere.RecalculateBounds();
	}
}
