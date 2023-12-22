using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeechBubbleContainer : SingletonBehavior<SpeechBubbleContainer>
{
    [SerializeField]
    SpeechBubble bubblePrefab;
    [SerializeField]
    int initialPoolSize = 3;

    Queue<SpeechBubble> bubbleQueue = new();
    List<SpeechBubble> returningBubbles = new();

    private void Start()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            returningBubbles.Add(RetrieveBubble());
        }
        foreach (var item in returningBubbles)
        {
            item.gameObject.SetActive(false);
            bubbleQueue.Enqueue(item);
        }
        returningBubbles.Clear();

        var lagSpikePreventer = SpawnAndBindBubble(null, "null");
        lagSpikePreventer.transform.position = Vector2.one * 999;
        DOTween.Sequence().AppendInterval(1).OnComplete(() => ReturnBubble(lagSpikePreventer));
    }

    public SpeechBubble SpawnAndBindBubble(Transform anchorTarget, string text, float yOffset = 8)
    {
        var spawned = RetrieveBubble();
        spawned.TargetPin.Target = anchorTarget;
        spawned.DisplayedText = text;
        spawned.TargetPin.screenOffset.y = yOffset;
        spawned.Visible = true;
        return spawned;
    }

    SpeechBubble RetrieveBubble()
    {
        SpeechBubble toReturn;
        if (bubbleQueue.Count > 0)
        {
            toReturn = bubbleQueue.Dequeue();
            toReturn.gameObject.SetActive(true);
            return toReturn;
        }
        toReturn = Instantiate(bubblePrefab, transform);
        return toReturn;
    }

    public void ReturnBubble(SpeechBubble toReturn)
    {
        if (returningBubbles.Contains(toReturn))
            return;

        returningBubbles.Add(toReturn);
        toReturn.Visible = false;
        var seq = DOTween.Sequence();
        seq.AppendInterval(0.25f);
        seq.AppendCallback(() =>
        {
            returningBubbles.Remove(toReturn);
            toReturn.gameObject.SetActive(false);
            bubbleQueue.Enqueue(toReturn);
        });
    }
}
