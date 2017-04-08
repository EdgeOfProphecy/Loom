using UnityEngine;
using System.Collections;
using Yarn.Unity;
using MagnetHillCo;

public class NPCDialogue : MonoBehaviour
{
    public TextAsset scriptToLoad;
    DialogueRunnerFS dr;

    public string startNode;
    private Animator anim;

    // Use this for initialization
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        dr = MHC.FindGameObjectComponentWithTag<DialogueRunnerFS>(FSTokens.TagDialogueSystemManager);

        if (scriptToLoad != null)
        {
            dr.AddScript(scriptToLoad);
        }
	}

    public void StartDialogue()
    {
        dr.speakerAnimator = anim;
        dr.StartDialogue(startNode);
    }
}
