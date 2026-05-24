using UnityEngine;

/// <summary>
/// Boss2 技能4 虚空突袭红圈预警。
/// 提供闪烁/脉动效果，在指定时间后自动销毁。
/// 可在 Inspector 中拖入自定义 sprite（如圆环），或留空自动生成。
/// </summary>
public class VoidWarningCircle : MonoBehaviour
{
    [Header("外观")]
    [Tooltip("预警持续时间（秒），到时间自动销毁")]
    public float duration = 2f;
    [Tooltip("闪烁频率（Hz）")]
    public float flashFrequency = 4f;
    [Tooltip("最小透明度")]
    [Range(0f, 1f)] public float minAlpha = 0.3f;
    [Tooltip("最大透明度")]
    [Range(0f, 1f)] public float maxAlpha = 0.8f;
    [Tooltip("旋转速度（度/秒）")]
    public float rotationSpeed = 180f;

    private SpriteRenderer _sr;
    private float _timer;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null)
            _sr = gameObject.AddComponent<SpriteRenderer>();

        if (_sr.sprite == null)
            _sr.sprite = CreateRingSprite(128, 0.85f);

        _sr.sortingOrder = 10;
        _sr.color = new Color(1f, 0.1f, 0.1f, maxAlpha);
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // 旋转
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        // 闪烁
        float flash = Mathf.PingPong(_timer * flashFrequency, 1f);
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, flash);
        _sr.color = new Color(1f, 0.1f, 0.1f, alpha);
    }

    private Sprite CreateRingSprite(int size, float fillRatio)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        float outerRadius = center * fillRatio;
        float innerRadius = outerRadius - size * 0.06f; // 环宽度

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= outerRadius && dist >= innerRadius)
                    pixels[y * size + x] = Color.white;
                else
                    pixels[y * size + x] = Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
