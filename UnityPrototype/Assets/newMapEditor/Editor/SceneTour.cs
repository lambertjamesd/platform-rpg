using UnityEngine;
using UnityEditor;
using System.Collections;

public class SceneTour {

	public delegate void TourAction(); 

	public static void TakeTour(string[] sceneList, TourAction action, bool saveAfterAction = false)
	{
		string startScene = EditorApplication.currentScene;
		EditorApplication.SaveScene();

		foreach (string scene in sceneList)
		{
			if (EditorApplication.OpenScene(scene))
			{
				action();

				if (saveAfterAction)
				{
					EditorApplication.SaveScene();
				}
			}
		}

		EditorApplication.OpenScene(startScene);
	}
}
