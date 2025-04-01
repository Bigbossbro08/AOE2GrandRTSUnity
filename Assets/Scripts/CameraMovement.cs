using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    Camera m_Camera;
    [Range(0f, 10f)]
    public float speed = 2.0f;
    bool isNavalMode = false;

    public void SetNavalMode(bool isNavalMode)
    {
        if (this.isNavalMode == isNavalMode) return;

        //if (m_Camera)
        //{
        //    float newSize = cachedOrthographicSize;
        //    if (isNavalMode)
        //    {
        //        m_Camera.orthographicSize = 20;
        //    }
        //    else
        //    {
        //        m_Camera.orthographicSize = 5;
        //    }
        //}
        //this.isNavalMode = isNavalMode;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        float speedMultiplier = 1 / m_Camera.orthographicSize;
        speedMultiplier *= speed;
        Vector3 deltaPos = Vector3.zero;
        if (Input.GetKey(KeyCode.A))
        {
            deltaPos += transform.right * -1 * speedMultiplier;
        }
        
        if (Input.GetKey(KeyCode.D))
        {
            deltaPos += transform.right * speedMultiplier;
        }
        
        if (Input.GetKey(KeyCode.W))
        {
            deltaPos += transform.forward * speedMultiplier;
        }
        
        if (Input.GetKey(KeyCode.S))
        {
            deltaPos += transform.forward * -1 * speedMultiplier;
        }

        transform.position += deltaPos;

        if (m_Camera)
        {
            if (Input.mouseScrollDelta.y < 0)
            {
                m_Camera.orthographicSize = Mathf.Clamp(m_Camera.orthographicSize + 1, isNavalMode ? 3 + 10 : 3, isNavalMode ? 15 + 10 : 15);
            }
            else if (Input.mouseScrollDelta.y > 0)
            {
                m_Camera.orthographicSize = Mathf.Clamp(m_Camera.orthographicSize - 1, isNavalMode ? 3 + 10 : 3, isNavalMode ? 15 + 10 : 15);
            }
        }
    }
}
