using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Camera))]
public class CameraAlignerEditor : Editor
{
    [MenuItem("CONTEXT/Camera/Align Scene View to This Camera")]
    private static void AlignToContextCamera(MenuCommand command)
    {
        Camera cam = (Camera)command.context;

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null || cam == null) return;

        sceneView.pivot = cam.transform.position + cam.transform.forward * 10f;
        sceneView.rotation = cam.transform.rotation;
        sceneView.size = 10f;
        sceneView.orthographic = cam.orthographic;

        if (cam.orthographic)
        {
            //sceneView..orthographicSize = cam.orthographicSize;
        }
        else
        {
            sceneView.camera.fieldOfView = cam.fieldOfView;
        }

        sceneView.Repaint();
    }
}