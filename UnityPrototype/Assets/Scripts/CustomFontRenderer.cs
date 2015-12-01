using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomFontRenderer : MonoBehaviour {
	public CustomFont font;
	public Material material;
	public string sortingLayer = "Default";
	public int sortingOrder = 0;
	public bool screenSpaceUnits = true;
	public float renderScale = 1.0f;

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

	public delegate GlyphVariation VariationCallback(int index);

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
	
	public Vector3 DrawTextScreen(Vector3 screenPos, string text, float horizontalAnchor = 0.0f, VariationCallback variationCallback = null)
	{
		Camera currentCamera = Camera.main;
		return DrawText(currentCamera.ViewportToWorldPoint(screenPos), text, horizontalAnchor, variationCallback);

	}

	public Vector3 DrawMultilineText(Vector3 worldPosition, float maxWidth, string text, float horizontalAnchor = 0.0f, VariationCallback variationCallback = null)
	{
		Camera currentCamera = Camera.main;
		Vector3 currentPosition = worldPosition;
		Vector3 right = currentCamera.transform.right;

		float scale = renderScale;
		
		if (screenSpaceUnits)
		{
			Vector3 offset = currentCamera.WorldToScreenPoint(currentPosition + right) - currentCamera.WorldToScreenPoint(currentPosition);
			scale *= font.PixelsPerUnit / offset.x;
		}

		CustomFont.FontCharacter spaceCharacter = font.GetCharacter(' ');

		string[] words = text.Split(' ');

		Vector3 horizontalPos = currentPosition;
		int currentIndex = 0;

		foreach (string word in words)
		{
			float wordWidth = font.MeasureWidth(word) * scale;

			if (wordWidth + horizontalPos.x - currentPosition.x > maxWidth) {
				horizontalPos.x = currentPosition.x;
				horizontalPos.y -= font.Height;
			}
			
			horizontalPos = DrawText(horizontalPos, word, 0.0f, (index) => variationCallback == null ? GlyphVariation.Default() : variationCallback(index + currentIndex));
			horizontalPos.x += spaceCharacter.WorldWidth * scale;

			currentIndex += word.Length + 1;
		}

		return horizontalPos;
	}

	public Vector3 DrawText(Vector3 worldPosition, string text, float horizontalAnchor = 0.0f, VariationCallback variationCallback = null)
	{
		Camera currentCamera = Camera.main;
		Quaternion cameraRotation = currentCamera.transform.rotation;
		Vector3 currentPosition = worldPosition;
		Vector3 right = currentCamera.transform.right;

		float scale = renderScale;

		if (screenSpaceUnits)
		{
			Vector3 offset = currentCamera.WorldToScreenPoint(currentPosition + right) - currentCamera.WorldToScreenPoint(currentPosition);
			scale *= font.PixelsPerUnit / offset.x;
		}

		if (horizontalAnchor != 0.0f)
		{
			float totalWidth = 0.0f;
			
			font.ForeachGlyph(text, (glyph, index) => {
				totalWidth += glyph.sprite.rect.width / glyph.sprite.pixelsPerUnit;
			});

			currentPosition -= (horizontalAnchor * scale * totalWidth) * right;
		}

		font.ForeachGlyph(text, (glyph, index) => {
			GlyphVariation variation;
			
			if (variationCallback == null)
			{
				variation = GlyphVariation.Default();
			}
			else
			{
				variation = variationCallback(index);
			}
			
			SpriteRenderer renderer = AllocRenderer();
			
			renderer.sprite = glyph.sprite;
			
			renderer.transform.rotation = cameraRotation;
			renderer.transform.position = currentPosition + (scale / glyph.sprite.pixelsPerUnit) * variation.screenOffset;
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
