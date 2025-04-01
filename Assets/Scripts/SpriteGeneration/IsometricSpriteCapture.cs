using UnityEngine;
using System.IO;

public class IsometricSpriteCapture : MonoBehaviour
{
    public Camera isoCamera;
    public Transform target;
    public int spriteSize = 512;

    private void Start()
    {
        CaptureSprite();
    }

    public void CaptureSprite()
    {
        RenderTexture rt = new RenderTexture(spriteSize, spriteSize, 24);
        isoCamera.targetTexture = rt;
        Texture2D screenshot = new Texture2D(spriteSize, spriteSize, TextureFormat.RGBA32, false);

        isoCamera.Render();
        RenderTexture.active = rt;
        screenshot.ReadPixels(new Rect(0, 0, spriteSize, spriteSize), 0, 0);
        screenshot.Apply();

        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Sprite.png", bytes);

        Debug.Log("Sprite Saved!");

        // Cleanup
        isoCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
    }
}
