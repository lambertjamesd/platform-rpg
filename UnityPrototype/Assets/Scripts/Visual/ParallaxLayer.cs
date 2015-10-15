using UnityEngine;
using System.Collections;

public class ParallaxLayer : MonoBehaviour {

	public float worldHeight = 20.0f;
	public Vector2 moveRatio = Vector2.one;
	public float verticalOffset = 0.0f;

	private Material material;
	private Vector2 imageSize;

	public void Start()
	{
		material = GetComponent<Renderer>().material;
		imageSize = new Vector2(material.mainTexture.width, material.mainTexture.height);
	}

	public void LateUpdate()
	{
		Camera camera = Camera.main;
		Vector3 layerPosition = camera.transform.position;
		layerPosition.y = moveRatio.y * (layerPosition.y + verticalOffset);
		layerPosition.z = transform.position.z;
		transform.position = layerPosition;

		Plane layerPlane = new Plane(camera.transform.forward, layerPosition);

		float hitDistance;
		Ray bottomLeftRay = camera.ViewportPointToRay(new Vector3(-1, -1, 0));
		layerPlane.Raycast(bottomLeftRay, out hitDistance);
		Vector3 bottomLeftWorld = bottomLeftRay.GetPoint(hitDistance);
		Vector3 bottomLeft = transform.InverseTransformPoint(bottomLeftWorld);

		Ray topRightRay = camera.ViewportPointToRay(new Vector3(1, 1, 0));
		layerPlane.Raycast(topRightRay, out hitDistance);
		Vector3 topRightWorld = topRightRay.GetPoint(hitDistance);
		Vector3 topRight = transform.InverseTransformPoint(topRightWorld);

		Vector3 currentScale = transform.localScale;
		transform.localScale = new Vector3(
			currentScale.x * (topRight.x - bottomLeft.x) * 0.5f, 
			worldHeight, 
			1.0f);

		float texUnitsInWorld = (topRightWorld.x - bottomLeftWorld.x) / worldHeight;
		float uvWidth = texUnitsInWorld * imageSize.y / imageSize.x;
		material.mainTextureOffset = new Vector2(uvWidth * moveRatio.x * layerPosition.x / (topRightWorld.x - bottomLeftWorld.x), 0);
		material.mainTextureScale = new Vector2(uvWidth * 0.5f, 1.0f);
	}
}
