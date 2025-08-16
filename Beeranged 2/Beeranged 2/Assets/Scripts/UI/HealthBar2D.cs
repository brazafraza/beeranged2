using UnityEngine;

public class HealthBar2D : MonoBehaviour
{
    public SpriteRenderer fill;          // the colored bar
    public SpriteRenderer background;    // optional dark bar under it
    public float yOffset = -0.4f;        // position under enemy
    public bool hideWhenFull = true;
    public float fadeDuration = 0.3f;    // fade out delay after last update

    private float _tSinceUpdate;

    void LateUpdate()
    {
        // keep bar oriented upright and under enemy
        transform.localRotation = Quaternion.identity;
        var p = transform.parent != null ? transform.parent.position : transform.position;
        transform.position = new Vector3(p.x, p.y + yOffset, transform.position.z);

        _tSinceUpdate += Time.deltaTime;
        if (hideWhenFull && _tSinceUpdate > fadeDuration)
        {
            SetVisible(false);
        }
    }

    public void Set(float fraction)
    {
        _tSinceUpdate = 0f;
        SetVisible(true);

        float f = Mathf.Clamp01(fraction);
        if (fill != null)
        {
            var s = fill.transform.localScale;
            s.x = Mathf.Max(0.0001f, f);
            fill.transform.localScale = s;
        }
    }

    public void SetInstant(float fraction)
    {
        _tSinceUpdate = 0f;
        if (!hideWhenFull || fraction < 0.999f) SetVisible(true);
        float f = Mathf.Clamp01(fraction);
        if (fill != null)
        {
            var s = fill.transform.localScale;
            s.x = Mathf.Max(0.0001f, f);
            fill.transform.localScale = s;
        }
    }

    private void SetVisible(bool v)
    {
        if (fill) fill.enabled = v;
        if (background) background.enabled = v;
    }
}
