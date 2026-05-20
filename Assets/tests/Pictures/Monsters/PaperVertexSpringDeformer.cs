using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public sealed class PaperVertexSpringDeformer : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.08f;
    [SerializeField] private float frequency = 3f;
    [SerializeField] private float waveSpeed = 2.4f;
    [SerializeField] private float horizontalDrift = 0.05f;
    [SerializeField] private bool useSegmentBias;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] deformedVertices;

    private void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        baseVertices = mesh.vertices;
        deformedVertices = new Vector3[baseVertices.Length];
    }

    private void Update()
    {
        float t = Time.time * waveSpeed;
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];
            float heightWeight = Mathf.Clamp01(vertex.y + 0.5f);
            float sideWeight = useSegmentBias ? Mathf.Abs(vertex.x) * 1.3f : 1f;
            float weight = heightWeight * sideWeight;
            float phase = vertex.y * frequency + vertex.x * 1.7f + t;
            vertex.x += Mathf.Sin(phase) * horizontalDrift * weight;
            vertex.z += Mathf.Cos(phase * 1.2f) * amplitude * weight;
            deformedVertices[i] = vertex;
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateBounds();
    }
}
