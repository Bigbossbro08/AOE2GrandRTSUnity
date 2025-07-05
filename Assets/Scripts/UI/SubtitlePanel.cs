using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SubtitlePanel : MonoBehaviour
{
    public static SubtitlePanel Instance;

    public TextMeshProUGUI textMesh;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        enabled = false;
        if (Instance.textMesh)
        {
            Instance.textMesh.enabled = false;
        }
    }

    public static bool SetText(string text)
    {
        if (Instance == null)
        {
            return false;
        }

        if (Instance.textMesh == null)
        {
            return false;
        }

        if (!Instance.textMesh.enabled)
        {
            Instance.textMesh.enabled = true;
        }
        Instance.textMesh.text = text;
        return true;
    }

    public static IEnumerator<IDeterministicYieldInstruction> SetTextWithDelay(string text, float delay)
    {
        SetText(text);
        yield return new DeterministicWaitForSeconds(delay);
        Instance.textMesh.enabled = false;
    }
}
