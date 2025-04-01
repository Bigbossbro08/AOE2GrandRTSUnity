using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSelectCollider : MonoBehaviour
{
    BoxCollider boxCollider;
    SpriteRenderer spriteRenderer;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        UpdateCollider();
    }

    void UpdateCollider()
    {
        if (spriteRenderer == null || boxCollider == null) return;

        // Get the sprite's world bounds
        Bounds spriteBounds = spriteRenderer.bounds;

        // Update collider to exactly match the sprite's size
        boxCollider.size = new Vector3(Mathf.Abs(spriteBounds.size.x), Mathf.Abs(spriteBounds.size.y), 0.1f);

        // Set collider center to match the sprite's exact center
        boxCollider.center = transform.InverseTransformPoint(spriteBounds.center);
    }
}
