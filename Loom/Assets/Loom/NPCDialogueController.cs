using UnityEngine;
using System.Collections;
using MagnetHillCo;
using Yarn.Unity;

public class NPCDialogueController : ContextSensitiveAction
{
    private NPCDialogue npcDialogue;

    void Awake()
    {
        npcDialogue = GetComponentInChildren<NPCDialogue>();
    }

    protected override void OnPerformContextSensitiveAction(Hashtable d)
    {
        npcDialogue.StartDialogue();
    }
}
