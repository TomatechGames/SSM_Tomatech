using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Globalization;
using System.Linq;
using DG.Tweening;
using UnityEngine.UI;

public class SpeechBubble : MonoBehaviour
{
    [SerializeField]
    PinToWorldTarget targetPin;
    public PinToWorldTarget TargetPin=>targetPin;

    [SerializeField]
    CanvasGroup canvasGroup;

    [SerializeField]
    [TextArea]
    string displayedText;
    public string DisplayedText
    {
        get => displayedText;
        set
        {
            displayedText = value;
            ResetProgress();
        }
    }

    bool visible;
    public bool Visible
    {
        get=> visible;
        set
        {
            if (visible != value)
            {
                if (visible)
                {
                    transform.DOScale(Vector3.one * 0.5f, 0.25f).SetEase(Ease.InCubic).OnComplete(()=>canvasGroup.alpha=0);
                }
                else
                {
                    transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutCubic);
                    canvasGroup.alpha = 1;
                }
            }
            visible = value;

        }
    }

    private void OnDisable()
    {
        visible= false;
        transform.localScale = Vector3.one * 0.5f;
        canvasGroup.alpha = 0;
    }

    [SerializeField]
    TextMeshProUGUI layoutText;
    [SerializeField]
    TextMeshProUGUI displayText;
    [SerializeField]
    AnimationCurve textAnimCurve;
    [SerializeField]
    float textAnimAmplitude;
    [SerializeField]
    float textAnimDuration;
    [SerializeField]
    float textAnimCharDelay;

    float progress = float.PositiveInfinity;
    int animationRange;
    float inverseDuration;
    float inverseDelay;

    private IEnumerator Start()
    {
        yield return null;
        ResetProgress();
    }

    [ContextMenu("Reset Text")]
    void ResetProgress()
    {
        progress = 0;
        inverseDelay = 1 / textAnimCharDelay;
        inverseDuration = 1 / textAnimDuration;
        animationRange = Mathf.CeilToInt(textAnimDuration * inverseDelay);

        layoutText.text = displayedText;
        displayText.text = layoutText.text;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);

        //var displayTextInfo = displayText.textInfo;
        //for (int i = 0; i < displayTextInfo.meshInfo.Length; i++)
        //{
        //    displayTextInfo.meshInfo[i].vertices = layoutText.textInfo.meshInfo[i].vertices;
        //}
        //UpdateAllDisplayGeometry(displayTextInfo);
        //layoutText.SetLayoutDirty();
    }

    void Update()
    {
        int animationOffset = Mathf.FloorToInt(Mathf.Max(progress - textAnimDuration, 0) * inverseDelay);


        if (!displayText || animationOffset >= displayText.textInfo.characterInfo.Length)
            return;
        layoutText.ForceMeshUpdate();
        displayText.ForceMeshUpdate();
        var layoutTextInfo = layoutText.textInfo;
        var displayTextInfo = displayText.textInfo;

        for (int i = animationOffset; i < Mathf.Min(animationOffset+animationRange, displayTextInfo.characterCount); i++)
        {
            var layoutCharInfo = layoutTextInfo.characterInfo[i];
            var displayCharInfo = displayTextInfo.characterInfo[i];
            if (!displayCharInfo.isVisible)
                continue;

            var layoutVerts = layoutTextInfo.meshInfo[layoutCharInfo.materialReferenceIndex].vertices;
            var displayVerts = displayTextInfo.meshInfo[displayCharInfo.materialReferenceIndex].vertices;

            for (int j = 0; j < 4; j++)
            {
                var basis = layoutVerts[layoutCharInfo.vertexIndex + j];
                displayVerts[displayCharInfo.vertexIndex + j] = basis + new Vector3(0, textAnimCurve.Evaluate((progress - (textAnimCharDelay * i)) * inverseDuration)* textAnimAmplitude, 0);
            }
        }

        for (int i = 0; i < displayTextInfo.meshInfo.Length; i++)
        {
            var meshInfo = displayTextInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            displayText.UpdateGeometry(meshInfo.mesh, i);
        }
        progress += Time.deltaTime;
    }
}
