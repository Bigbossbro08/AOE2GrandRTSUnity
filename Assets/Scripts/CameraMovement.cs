using UnityEditor;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    Camera m_Camera;
    [Range(0f, 100f)]
    public float speed = 2.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Camera = Camera.main;
        Cursor.lockState = CursorLockMode.Confined;
    }

    bool IsCursorNotProperlyConfined()
    {
        Vector2 pos = Input.mousePosition;
        bool isInsideScreen = pos.x >= 0 && pos.x <= Screen.width &&
                              pos.y >= 0 && pos.y <= Screen.height;

#if UNITY_EDITOR
        bool isGameViewFocused = EditorWindow.mouseOverWindow != null &&
                                 EditorWindow.mouseOverWindow.titleContent != null &&
                                 EditorWindow.mouseOverWindow.titleContent.text == "Game";
        return !isInsideScreen || !isGameViewFocused;
#else
    return !isInsideScreen;
#endif
    }

    // Update is called once per frame
    void Update()
    {
        if (IsCursorNotProperlyConfined()) return;

        const int borderThickness = 5;
        bool moveRight = false;
        bool moveLeft = false;
        bool moveUp = false;
        bool moveDown = false;
        if (Input.mousePosition.x < borderThickness)
        {
            moveLeft = true;
        }
        if (Input.mousePosition.x > Screen.width - borderThickness)
        {
            moveRight = true;
        }

        if (Input.mousePosition.y < borderThickness)
        {
            moveUp = true;
        }
        if (Input.mousePosition.y > Screen.height - borderThickness)
        {
            moveDown = true;
        }

        float speedMultiplier = 1 / m_Camera.orthographicSize;
        speedMultiplier *= speed;
        Vector3 deltaPos = Vector3.zero;
        //if (Input.GetKey(KeyCode.A))
        if (moveLeft)
        {
            deltaPos += transform.right * -1 * speedMultiplier;
        }
        
        //if (Input.GetKey(KeyCode.D))
        if (moveRight)
        {
            deltaPos += transform.right * speedMultiplier;
        }
        
        //if (Input.GetKey(KeyCode.W))
        if (moveDown)
        {
            deltaPos += transform.forward * speedMultiplier;
        }
        
        //if (Input.GetKey(KeyCode.S))
        if (moveUp)
        {
            deltaPos += transform.forward * -1 * speedMultiplier;
        }

        transform.position += deltaPos * Time.deltaTime;

        if (m_Camera)
        {
            if (Input.mouseScrollDelta.y < 0)
            {
                m_Camera.orthographicSize = Mathf.Clamp(m_Camera.orthographicSize + 1, 3, 15);
            }
            else if (Input.mouseScrollDelta.y > 0)
            {
                m_Camera.orthographicSize = Mathf.Clamp(m_Camera.orthographicSize - 1, 3, 15);
            }
        }
    }
}
