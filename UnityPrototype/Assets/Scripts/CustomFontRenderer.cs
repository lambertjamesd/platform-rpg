﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomFontRenderer : MonoBehaviour {
	public CustomFont font;
	public Material material;
	public string sortingLayer = "Default";
	public int sortingOrder = 0;
	public bool screenSpaceUnits = true;

	private List<SpriteRenderer> glyphCache = new List<SpriteRenderer>();
	private int currentGlyphIndex = 0;

	public const float LeftAlign = 0.0f;
	public const float CenterAlign = 0.5f;
	public const float RightAlign = 1.0f;

	public struct GlyphVariation
	{
		public Vector3 screenOffset;
		public Color color;
		public float scale;

		public GlyphVariation(Vector3 screenOffset, Color color, float scale)
		{
			this.screenOffset = screenOffset;
			this.color = color;
			this.scale = scale;
		}

		public static GlyphVariation Default()
		{
			return new GlyphVariation(Vector3.zero, Color.white, 1.0f);
		}
	}

	public delegate GlyphVariation VariationCallback();

	private SpriteRenderer AllocRenderer()
	{
		SpriteRenderer result = null;

		if (currentGlyphIndex == glyphCache.Count)
		{
			GameObject anchor = new GameObject();
			anchor.transform.parent = transform;
			result = anchor.AddComponent<SpriteRenderer>();
			result.material = material;
			result.sortingLayerName = sortingLayer;
			result.sortingOrder = sortingOrder;

			glyphCache.Add(result);

		}
		else
		{
			result = glyphCache[currentGlyphIndex];
		}

		++currentGlyphIndex;

		return result;
	}

	public Vector3 DrawText(Vector3 worldPosition, string text, float horizontalAnchor = 0.0f, VariationCallback variationCallback = null)
	{
		Camera currentCamera = Camera.main;
		Quaternion cameraRotation = currentCamera.transform.rotation;
		Vector3 currentPosition = worldPosition;
		Vector3 right = currentCamera.transform.right;

		float scale = 1.0f;

		if (screenSpaceUnits)
		{
			Vector3 offset = currentCamera.WorldToScreenPoint(currentPosition + right) - currentCamera.WorldToScreenPoint(currentPosition);
			scale = font.PixelsPerUnit / offset.x;
		}

		if (horizontalAnchor != 0.0f)
		{
			float totalWidth = 0.0f;
			
			font.ForeachGlyph(text, glyph => {
				totalWidth += glyph.sprite.rect.width / glyph.sprite.pixelsPerUnit;
			});

			currentPosition -= (horizontalAnchor * scale * totalWidth) * right;
		}

		font.ForeachGlyph(text, glyph => {
			GlyphVariation variation;
			
			if (variationCallback == null)
			{
				variation = GlyphVariation.Default();
			}
			else
			{
				variation = variationCallback();
			}
			
			SpriteRenderer renderer = AllocRenderer();
			
			renderer.sprite = glyph.sprite;
			
			renderer.transform.rotation = cameraRotation;
			renderer.transform.position = currentPosition;
			renderer.transform.localScale = new Vector3(variation.scale * scale, variation.scale * scale, 1.0f);
			
			renderer.color = variation.color;
			
			currentPosition += right * (scale * glyph.sprite.rect.width / glyph.sprite.pixelsPerUnit);
			
			renderer.gameObject.SetActive(true);
		});

		return currentPosition;
	}

	public void LateUpdate()
	{
		while (currentGlyphIndex < glyphCache.Count)
		{
			glyphCache[currentGlyphIndex].gameObject.SetActive(false);
			++currentGlyphIndex;
		}

		currentGlyphIndex = 0;
	}
}
