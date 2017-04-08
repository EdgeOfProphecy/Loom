using UnityEngine;
using System.Collections;
using Yarn.Unity;
using Yarn;

public class DialogueAPIExtenderFS : MonoBehaviour
{
    Dialogue dialogue;
    DialogueRunnerFS dialogueRunner;
    NPCDialogueUIFS npcDialogueUI;

    private Inventory playerInventory;

    void Awake()
    {
        NotificationCenter.DefaultCenter.AddObserver(FSTokens.NotifPlayerSpawned, OnPlayerSpawned);

        dialogueRunner = GetComponent<DialogueRunnerFS>();
        dialogue = dialogueRunner.dialogue;
        dialogue.library.RegisterFunction("has_item", 2, delegate (Value[] parameters)
        {
            int itemID = (int)parameters[0].AsNumber;
            int itemQuantity = (int)parameters[1].AsNumber;

            return playerInventory.CanAffordItemDeduction((ItemDatabase.ItemIDs)itemID, itemQuantity);
        });
        dialogue.library.RegisterFunction("deduct_item", 2, delegate (Value[] parameters)
        {
            Debug.Log("Deducting item");
            int itemID = (int)parameters[0].AsNumber;
            int itemQuantity = (int)parameters[1].AsNumber;

            return playerInventory.RemoveItem((ItemDatabase.ItemIDs)itemID, itemQuantity);
        });
        dialogue.library.RegisterFunction("set_animation_trigger", 1, delegate (Value[] parameters)
        {
            dialogueRunner.speakerAnimator.SetTrigger((string)parameters[0].AsString);
            return null;
        });
        dialogue.library.RegisterFunction("text_speed", 1, delegate (Value[] parameters)
        {
            npcDialogueUI.textSpeedIndex = (int)parameters[0].AsNumber;
            return null;
        });
    }

    void Start()
    {
        npcDialogueUI = MagnetHillCo.MHC.FindGameObjectComponentWithTag<NPCDialogueUIFS>(FSTokens.TagNPDialogueUIRoot);
    }

    void OnPlayerSpawned(Hashtable d)
    {
        playerInventory = ((GameObject)d[FSTokens.DictKeySelf]).GetComponent<Inventory>();
    }
}
