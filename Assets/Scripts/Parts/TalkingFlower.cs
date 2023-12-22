using Aarthificial.Reanimation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalkingFlower : MonoBehaviour
{
    [SerializeField]
    [TextArea(3, 3)]
    string message;
    [SerializeField]
    Reanimator reanimator;
    [SerializeField]
    AudioClip talkSound;

    SpeechBubble activeBubble;

    public void OpenMessage()
    {
        reanimator.Set("isTalking", 1);
        activeBubble = SpeechBubbleContainer.Instance.SpawnAndBindBubble(transform, message);
        if(talkSound)
            AudioSource.PlayClipAtPoint(talkSound, transform.position);
    }

    public void CloseMessage()
    {
        reanimator.Set("isTalking", 0);
        if (activeBubble)
            SpeechBubbleContainer.Instance.ReturnBubble(activeBubble);
    }
}
