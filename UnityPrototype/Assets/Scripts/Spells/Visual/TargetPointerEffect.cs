using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TargetPointerEffect : EffectGameObject {
	private int visualizerLimit;
	private int collideWith;
	private List<GameObject> currentInstances = new List<GameObject>();
	private GameObject template;
	private PlayerManager playerManager;

	public override void StartEffect(EffectInstance instance) {
		base.StartEffect(instance);
		gameObject.GetOrAddComponent<TimeGameObject>();

		visualizerLimit = instance.GetValue<int>("limit");
		collideWith = instance.GetValue<int>("collideWith");
		template = instance.GetPrefab("template");
		playerManager = instance.GetContextValue<PlayerManager>("playerManager", null);
	}

	public void Update()
	{
		if (playerManager != null && template != null)
		{
			Vector3 source = transform.position;

			IEnumerable<object> exclude = (IEnumerable<object>)instance.GetValue<object>("exclude", new List<object>());

			IEnumerable<Vector3> finalTargets = playerManager.PlayersOnLayers(collideWith)
				.Where(player => !exclude.Contains(player.gameObject) && player.gameObject != gameObject.GetParent())
				.Select(player => player.transform.position)
				.OrderBy(position => (position - source).sqrMagnitude);

			int index = 0;
			foreach (Vector3 target in finalTargets)
			{
				if (index == visualizerLimit)
				{
					break;
				}

				if (currentInstances.Count == index)
				{
					GameObject newCopy = (GameObject)Instantiate(template, transform.position, Quaternion.identity);
					newCopy.transform.parent = transform;
					currentInstances.Add(newCopy);
				}

				if (!currentInstances[index].activeSelf)
				{
					currentInstances[index].SetActive(true);
				}

				currentInstances[index].transform.rotation = Quaternion.LookRotation(Vector3.forward, target - source);
				++index;
			}

			while (index < currentInstances.Count)
			{
				currentInstances[index].SetActive(false);
				++index;
			}
		}
	}
}
