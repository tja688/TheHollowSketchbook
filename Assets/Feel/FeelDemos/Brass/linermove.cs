using UnityEngine;

[ExecuteInEditMode]
public class LinearMover : MonoBehaviour
{
    [Header("端点设置")]
    [SerializeField] private Vector3 pointA = new Vector3(-5f, 0f, 0f);
    [SerializeField] private Vector3 pointB = new Vector3(5f, 0f, 0f);

    [Header("移动参数")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private bool pingPong = true;
    [SerializeField, Range(0f, 1f)] private float startProgress;

    [Header("Gizmos 颜色")]
    [SerializeField] private Color lineColor = new Color(0f, 1f, 1f, 0.5f);
    [SerializeField] private float pointRadius = 0.15f;

    private float t;
    private int dir = 1;

    void Start()
    {
        t = startProgress;
        transform.position = Vector3.Lerp(pointA, pointB, t);
    }

    void Update()
    {
        float distance = Vector3.Distance(pointA, pointB);
        if (distance < 0.001f) return;

        float delta = (speed * Time.deltaTime) / distance;
        t += delta * dir;

        if (t >= 1f)
        {
            t = 1f;
            dir = pingPong ? -1 : -1; // 到达B端折返
        }
        else if (t <= 0f)
        {
            t = 0f;
            dir = pingPong ? 1 : 1;  // 到达A端折返
        }

        transform.position = Vector3.Lerp(pointA, pointB, t);
    }

    void OnDrawGizmos()
    {
        if (!enabled) return;

        // 绘制线段
        Gizmos.color = lineColor;
        Gizmos.DrawLine(pointA, pointB);

        // 端点 A（绿）
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pointA, pointRadius);

        // 端点 B（红）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pointB, pointRadius);

        // 当前位置预览（编辑模式）
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 preview = Vector3.Lerp(pointA, pointB, startProgress);
            Gizmos.DrawWireCube(preview, Vector3.one * pointRadius);
        }
    }

    void OnDrawGizmosSelected()
    {
        // 选中时加粗显示
        Gizmos.color = lineColor;
        Gizmos.DrawLine(pointA, pointB);
    }
}