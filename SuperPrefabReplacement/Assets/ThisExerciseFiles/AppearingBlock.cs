using UnityEngine;

public class AppearingBlock : MonoBehaviour
{
    public int m_GroupNumber;

    // Called from SuperTiled2Unity Replace Prefabs feature
    public void SetGroupNumber(int number)
    {
        m_GroupNumber = number;
    }
}

