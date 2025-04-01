using UnityEngine;
using System.Collections;

//public class SLDLoader : MonoBehaviour
//{
//    // Set the SLD file path (for example, "Assets/StreamingAssets/myfile.sld")
//    public string filePath = "E:\\Games\\steamapps\\common\\AoE2DE\\resources\\_common\\drs\\graphics\\u_inf_strategos_idleA_x2.sld";
//    // Assign a Renderer (e.g., a Quad or UI RawImage) in the inspector to display the texture
//    public Renderer targetRenderer;
//
//    void Start()
//    {
//        try
//        {
//            SLDReader reader = new SLDReader(filePath);
//            if (reader.texture == null)
//            {
//                Debug.LogError("Texture is null after reading SLD file.");
//                return;
//            }
//            if (targetRenderer != null)
//            {
//                targetRenderer.material.mainTexture = reader.texture;
//            }
//            else
//            {
//                Debug.LogWarning("Target renderer not assigned.");
//            }
//        }
//        catch (System.Exception ex)
//        {
//            Debug.LogError("Error loading SLD file: " + ex.Message);
//        }
//    }
//}

public class SLDSpritePlayer : MonoBehaviour
{
    //public string sldFilePath = "Assets/StreamingAssets/sprite.sld";
    public string sldFilePath = "E:\\Games\\steamapps\\common\\AoE2DE\\resources\\_common\\drs\\graphics\\u_inf_strategos_idleA_x2.sld";
    public SpriteRenderer targetSpriteRenderer;
    public float frameRate = 10f; // frames per second

    private SLDReader sldReader;
    private Sprite[] sprites;
    private int currentFrame = 0;

    IEnumerator Start()
    {
        // Load SLD frames
        sldReader = new SLDReader(sldFilePath);
        Texture2D[] frames = sldReader.frameTextures;
        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("No frames loaded from SLD file.");
            yield break;
        }

        // Pack the frames into an atlas.
        // Adjust atlas size and padding as needed.
        Texture2D atlas = new Texture2D(2048, 2048, TextureFormat.RGBA32, false);
        // PackTextures returns normalized UV rects for each texture.
        Rect[] rects = atlas.PackTextures(frames, 2, 2048);

        // Create sprites from atlas using the rects.
        sprites = new Sprite[frames.Length];
        int atlasWidth = atlas.width;
        int atlasHeight = atlas.height;
        for (int i = 0; i < frames.Length; i++)
        {
            Rect r = rects[i];
            // Convert normalized rect to pixel coordinates.
            float x = r.x * atlasWidth;
            float y = r.y * atlasHeight;
            float width = r.width * atlasWidth;
            float height = r.height * atlasHeight;
            // Create the sprite; adjust the pixelsPerUnit as needed.
            sprites[i] = Sprite.Create(atlas, new Rect(x, y, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        // Set the first sprite.
        if (targetSpriteRenderer != null)
            targetSpriteRenderer.sprite = sprites[0];
        else
            Debug.LogError("No SpriteRenderer assigned.");

        // Animate by cycling through sprites.
        while (true)
        {
            yield return new WaitForSeconds(1f / frameRate);
            currentFrame = (currentFrame + 1) % sprites.Length;
            targetSpriteRenderer.sprite = sprites[currentFrame];
        }
    }
}
