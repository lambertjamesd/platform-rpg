using UnityEngine;
using System.Collections;

public abstract class CustomCollider : MonoBehaviour {
	protected MoveSignal moveSignal;
	protected SpacialIndex index;
	
	public int collisionGroup = -1;
	public int collisionLayers = ~0;

	public static void AddToIndex(GameObject target, SpacialIndex newIndex)
	{
		foreach (CustomCollider collider in target.GetComponents<CustomCollider>())
		{
			collider.AddToIndex(newIndex);
		}
	}

	public abstract void InitializeShape();
	public abstract void UpdateProperties();

	public void AddToIndex(SpacialIndex newIndex)
	{
		if (newIndex != index)
		{
			if (index != null)
			{
				index.RemoveShape(GetShape());
			}

			if (GetShape() == null)
			{
				InitializeShape();
			}

			index = newIndex;
			UpdateIndex();
		}
	}

	void OnEnable()
	{
		if (moveSignal == null)
		{
			moveSignal = gameObject.GetOrAddComponent<MoveSignal>();
		}

		moveSignal.Listen(UpdateIndex);
	}

	void OnDisable()
	{
		moveSignal.Unlisten(UpdateIndex);

		if (index != null)
		{
			index.RemoveShape(GetShape());
		}
	}

	public virtual void UpdateIndex()
	{
		if (index != null)
		{
			index.IndexShape(GetShape());
		}
	}

	public abstract ICollisionShape GetShape();
}
