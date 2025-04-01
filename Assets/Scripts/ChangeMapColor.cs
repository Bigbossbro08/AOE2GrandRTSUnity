using UnityEngine;

public class ChangeMapColor : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer)
        {
            Texture2D texture = spriteRenderer.sprite.texture;

            if (texture != null )
            {

                // Create a copy of the texture to avoid modifying the original asset
                Texture2D newTexture = new Texture2D(texture.width, texture.height);
                newTexture.SetPixels(texture.GetPixels()); // Copy pixels
                newTexture.Apply();

                Color[] pixels = texture.GetPixels();
                for ( int i = 0; i < pixels.Length; i++ )
                {
                    Color currentColor = pixels[i];

                    if (currentColor.r > 0)
                    {
                        // Grasslands
                        ColorUtility.TryParseHtmlString("#2E6F40", out Color newColor);
                        pixels[i] = newColor;
                    }
                    else
                    {
                        // Forest
                        ColorUtility.TryParseHtmlString("#2C5F34", out Color newColor);
                        pixels[i] = newColor;
                    }
                }
                newTexture.SetPixels(pixels);
                newTexture.Apply();
                spriteRenderer.sprite = Sprite.Create(newTexture, spriteRenderer.sprite.rect, new Vector2(0.5f, 0.5f));
            }
        } 
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 texturePixelPos = GetTexturePixelPosition(mouseWorldPos);
            Debug.Log("Raw Texture Pixel Position: " + texturePixelPos);
        }
    }
    
    Vector2 GetPixelPositionFromRaycast(Vector2 hitPoint, SpriteRenderer spriteRenderer)
    {
        // Convert world hit point to local space of the sprite
        Vector2 localPoint = spriteRenderer.transform.InverseTransformPoint(hitPoint);

        Sprite sprite = spriteRenderer.sprite;
        Texture2D texture = sprite.texture;

        // Convert local position to sprite pixel coordinates
        float pixelsPerUnit = sprite.pixelsPerUnit;
        Vector2 spritePixelPos = new Vector2(localPoint.x * pixelsPerUnit, localPoint.y * pixelsPerUnit);

        // Adjust for sprite's position within the texture (sprite.rect)
        Rect spriteRect = sprite.rect;
        Vector2 texturePixelPos = new Vector2(spriteRect.x + spritePixelPos.x, spriteRect.y + spritePixelPos.y);

        // Clamp to prevent out-of-bounds errors
        texturePixelPos.x = Mathf.Clamp(texturePixelPos.x, 0, texture.width - 1);
        texturePixelPos.y = Mathf.Clamp(texturePixelPos.y, 0, texture.height - 1);

        return texturePixelPos;
    }

    Vector2 GetTexturePixelPosition(Vector3 worldPosition)
    {
        if (spriteRenderer == null)
        {
            Debug.Log("Sprite Renderer doesnt exist!");
            return Vector2.zero;
        }
        // Convert world position to local position relative to the sprite
        Vector3 localPos = spriteRenderer.transform.InverseTransformPoint(worldPosition);

        // Get the sprite data
        Sprite sprite = spriteRenderer.sprite;
        Texture2D texture = sprite.texture;

        // Calculate the position in sprite space (in pixels)
        float pixelsPerUnit = sprite.pixelsPerUnit;
        Vector2 spritePixelPos = new Vector2(localPos.x * pixelsPerUnit, localPos.y * pixelsPerUnit);

        // Now adjust for the sprite's rect in the texture
        Rect spriteRect = sprite.rect;
        Vector2 rawTexturePos = new Vector2(spriteRect.x + spritePixelPos.x, spriteRect.y + spritePixelPos.y);

        // Clamp to texture bounds to avoid out-of-range errors
        rawTexturePos.x = Mathf.Clamp(rawTexturePos.x, 0, texture.width - 1);
        rawTexturePos.y = Mathf.Clamp(rawTexturePos.y, 0, texture.height - 1);

        return rawTexturePos;
    }
}
