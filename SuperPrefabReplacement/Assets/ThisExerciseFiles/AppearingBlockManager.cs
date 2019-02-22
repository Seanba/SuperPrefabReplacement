using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AppearingBlockManager : MonoBehaviour
{
    private List<AppearingBlock> m_AppearingBlocks = new List<AppearingBlock>();

    private void Start()
    {
        // Gather our list of appearing blocks
        // And start each block off as inactive/invisible
        foreach (var go in GameObject.FindGameObjectsWithTag("AppearingBlock"))
        {
            go.SetActive(false);
            var block = go.GetComponent<AppearingBlock>();
            if (block != null)
            {
                m_AppearingBlocks.Add(block);
            }
        }

        StartCoroutine(ManageBlocks());
    }

    private IEnumerator ManageBlocks()
    {
        yield return new WaitForSeconds(2.0f);

        // Cycle through our groups
        while (true)
        {
            // Group 0 now active
            foreach (var block in m_AppearingBlocks)
            {
                block.gameObject.SetActive(block.m_GroupNumber == 0);
            }

            yield return new WaitForSeconds(2.0f);

            // Group 1 now active
            foreach (var block in m_AppearingBlocks)
            {
                block.gameObject.SetActive(block.m_GroupNumber == 1);
            }

            yield return new WaitForSeconds(2.0f);

            // Group 2 now active
            foreach (var block in m_AppearingBlocks)
            {
                block.gameObject.SetActive(block.m_GroupNumber == 2);
            }

            yield return new WaitForSeconds(2.0f);
        }
    }
}

