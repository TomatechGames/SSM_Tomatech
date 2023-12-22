using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tomatech.RePalette;
using UnityEngine;

public class ThemedParticleRetriever : MonoBehaviour, IThemeable
{
    public ParticleSet[] particleSets;

    private void Awake()
    {
        RepaletteResourceManager.RegisterThemeable(this);
        UpdateThemeContent().JustRun();
    }

    private void OnDestroy()
    {
        RepaletteResourceManager.UnregisterThemeable(this);
    }

    public async Task UpdateThemeContent()
    {
        await Task.WhenAll(particleSets.Select(s=>s.ApplySprites()).ToArray());
    }

    [Serializable]
    public class ParticleSet
    {
        public ParticleSystem system;
        public ThemeAssetReference<Sprite>[] sprites;

        public async Task ApplySprites()
        {
            var textures = system.textureSheetAnimation;
            for (int i = 0; i < sprites.Length; i++)
            {
                var item = sprites[i];
                var assetTask = item?.GetAsset();
                if (assetTask != null)
                {
                    await assetTask;
                    textures.SetSprite(i, assetTask.Result);
                }
                else
                    textures.SetSprite(i, null);
            }
        }
    }
}
