using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace BrickStacker
{
    public partial class MenuController : MonoBehaviour
    {
        System.Collections.IEnumerator PulseButton(Transform btn, Transform shadow)
        {
            float amplitude = 0.030f;
            float speed = 0.65f;
            while (btn != null)
            {
                float s = 1f + amplitude * Mathf.Sin(Time.time * speed * Mathf.PI * 2f);
                btn.localScale = new Vector3(s, s, 1f);
                if (shadow != null) shadow.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        void AddPressScaleFeedback(GameObject target, float pressedScale)
        {
            var feedback = target.GetComponent<PressScaleFeedback>() ?? target.AddComponent<PressScaleFeedback>();
            feedback.PressedScale = pressedScale;
        }

        void StyleMapBackButton(Button button)
        {
            var background = button.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0f);
            background.sprite = null;
            background.type = Image.Type.Simple;

            var icon = Ui.Panel(button.transform, "Close Icon", Color.white).GetComponent<Image>();
            icon.sprite = RuntimeArt.LoadV3SubSprite("popup-1vs1/btn-close.png", new Rect(0.309f, 0.254f, 0.382f, 0.560f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Ui.Rect(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(44, 44));

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.58f, 1f);
            colors.pressedColor = new Color(0.78f, 0.46f, 0.20f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
        }

        void AddDarkWoodTextEdge(Text text, float thickness, float alpha)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.13f, 0.050f, 0.014f, alpha);
            outline.effectDistance = new Vector2(thickness, thickness);
            outline.useGraphicAlpha = true;

            var depth = text.gameObject.AddComponent<Shadow>();
            depth.effectColor = new Color(0.045f, 0.016f, 0.005f, 0.62f);
            depth.effectDistance = new Vector2(0.75f, -0.85f);
            depth.useGraphicAlpha = true;
        }

        void AddWarmTitleFinish(Text text, float glowStrength)
        {
            text.fontStyle = FontStyle.Bold;

            var topGlow = text.gameObject.AddComponent<Shadow>();
            topGlow.effectColor = new Color(1f, 0.68f, 0.30f, 0.26f * glowStrength);
            topGlow.effectDistance = new Vector2(-0.55f, 0.65f);
            topGlow.useGraphicAlpha = true;

            var carvedDrop = text.gameObject.AddComponent<Shadow>();
            carvedDrop.effectColor = new Color(0.035f, 0.012f, 0.004f, 0.78f);
            carvedDrop.effectDistance = new Vector2(0.8f, -0.9f);
            carvedDrop.useGraphicAlpha = true;
        }

        void StyleWoodRectButton(Button button, int fontSize)
        {
            var image = button.GetComponent<Image>();
            image.sprite = RuntimeArt.CreateWoodButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var text = button.GetComponentInChildren<Text>();
            text.color = new Color(1f, 0.90f, 0.68f);
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextMinSize = Mathf.Min(16, fontSize);
            text.resizeTextMaxSize = fontSize;
            AddDarkWoodTextEdge(text, 0.95f, 0.88f);

            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.055f, 0.018f, 0.006f, 0.58f);
            shadow.effectDistance = new Vector2(0.8f, -0.9f);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.74f, 1f);
            colors.pressedColor = new Color(0.78f, 0.52f, 0.30f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
        }

        void StyleWoodPopupFrame(GameObject panel)
        {
            var image = panel.GetComponent<Image>();
            image.sprite = RuntimeArt.CreateWoodPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
    }
}
