using UnityEngine;
using System.Collections;

namespace Yarn.Unity
{
    [AddComponentMenu("Scripts/Yarn Spinner/Dialogue Runner FS")]
    public class DialogueRunnerFS : DialogueRunner
    {
        public Animator speakerAnimator;

        protected override void Start()
        {
            dialogueUI = MagnetHillCo.MHC.FindGameObjectComponentWithTag<DialogueUIBehaviour>(FSTokens.TagNPDialogueUIRoot);

            base.Start();
        }
    }
}