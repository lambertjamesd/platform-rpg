using UnityEngine;
using System.Collections;

public class AimIndicatorUpdate : EffectObject 
{
	public override void StartEffect(EffectInstance instance)
	{
		base.StartEffect(instance);
		AimIndicator target = instance.GetValue<AimIndicator>("target", null);

		if (target != null)
		{
			target.InitialVelocity = instance.GetValue<Vector3>("direction", Vector3.zero).normalized * instance.GetValue<float>("speed", 0.0f);
			target.ChargeAmount = instance.GetValue<float>("normalizedHoldTime", 0.0f);
		}
	}
}

public class AimIndicator : EffectGameObject, ITimeTravelable
{
	public Gradient chargeColor = new Gradient();

	public float predectionDuration = 1.0f;

	private bool useGravity;

	private float chargeAmount;
	private Vector2 initialVelocity;
	private TimeManager timeManager;

	private static readonly int DEFAULT_STRIP_RESOLUTION = 16;

	private static Mesh BuildMesh(int resolution)
	{
		Mesh result = new Mesh();

		int pointCount = resolution * 2;

		Vector3[] vertexPositions = new Vector3[pointCount];

		int currentIndex = 0;
		float xPosition = 0.0f;
		float xStep = 1.0f / (resolution - 1);

		while (currentIndex < pointCount)
		{
			vertexPositions[currentIndex + 0] = new Vector3(xPosition, 1.0f, 0.0f);
			vertexPositions[currentIndex + 1] = new Vector3(xPosition, -1.0f, 0.0f);

			currentIndex += 2;
			xPosition += xStep;
		}

		int indexCount = (resolution - 1) * 6;
		int[] indices = new int[indexCount];

		currentIndex = 0;
		int vertexOffset = 0;
		while (currentIndex < indexCount)
		{
			indices[currentIndex + 0] = vertexOffset + 0;
			indices[currentIndex + 1] = vertexOffset + 2;
			indices[currentIndex + 2] = vertexOffset + 1;
			indices[currentIndex + 3] = vertexOffset + 1;
			indices[currentIndex + 4] = vertexOffset + 2;
			indices[currentIndex + 5] = vertexOffset + 3;

			currentIndex += 6;
			vertexOffset += 2;
		}

		result.subMeshCount = 1;

		result.vertices = vertexPositions;
		result.SetIndices(indices, MeshTopology.Triangles, 0);

		result.Optimize();

		return result;
	}

	private static Mesh stripMesh = null;

	public static Mesh GetStripMesh()
	{
		if (stripMesh == null)
		{
			stripMesh = BuildMesh(DEFAULT_STRIP_RESOLUTION);
		}

		return stripMesh;
	}

	public float ChargeAmount
	{
		get
		{
			return chargeAmount;
		}

		set
		{
			chargeAmount = value;
			GetComponent<Renderer>().material.SetColor("_ChargeColor", chargeColor.Evaluate(value));
			GetComponent<Renderer>().material.SetFloat ("_ChargeAmount", value);
		}
	}

	public Vector2 InitialVelocity
	{
		get
		{
			return initialVelocity;
		}

		set
		{
			initialVelocity = value;

			Vector3 gravity = useGravity ? Physics.gravity * predectionDuration * predectionDuration * 0.5f : Vector3.zero;

			GetComponent<Renderer>().material.SetVector("_PathCoeff", new Vector4(
				value.x * predectionDuration, 
				gravity.x, 
				value.y * predectionDuration, 
				gravity.y)); 
		}
	}

	public override void StartEffect(EffectInstance instance)
	{
		base.StartEffect (instance);
		MeshFilter filter = gameObject.GetOrAddComponent<MeshFilter>();

		filter.sharedMesh = filter.sharedMesh ?? GetStripMesh();

		useGravity = instance.GetValue<bool>("useGravity", false);

		timeManager = instance.GetContextValue<TimeManager>("timeManager", null);

		if (timeManager != null)
		{
			timeManager.AddTimeTraveler(this);
		}
	}

	public override IEffectPropertySource PropertySource {
		get {
			IEffectPropertySource parentSource = base.PropertySource;

			return new LambdaPropertySource(name => {
				if (name == "effect")
				{
					return this;
				}
				else
				{
					return parentSource.GetObject(name);
				}
			});
		}
	}

	public object GetCurrentState()
	{
		return null;
	}

	public void RewindToState(object state)
	{
		Destroy(gameObject);
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}
