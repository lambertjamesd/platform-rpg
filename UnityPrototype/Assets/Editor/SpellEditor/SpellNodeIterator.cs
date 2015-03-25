using UnityEngine;
using System.Collections;

public class SpellNodeIterator {
	public delegate void NodeCallback(SpellNode node);
	public delegate void ConnectorCallback(SpellConnection node);

	NodeCallback effectNodeCallback;
	NodeCallback expressionNodeCallback;

	ConnectorCallback connectionCallback;
	
	public SpellNodeIterator()
	{

	}

	public void SetEffectNodeCallback(NodeCallback value)
	{
		effectNodeCallback = value;
	}

	public void SetExpressionNodeCallback(NodeCallback value)
	{
		expressionNodeCallback = value;
	}

	public void SetConnectionCallback(ConnectorCallback value)
	{
		connectionCallback = value;
	}

	private void VisitConnection(SpellConnection connection)
	{
		if (connectionCallback != null)
		{
			connectionCallback(connection);
		}
	}

	private void EnterTriggerEvent(SpellNodeConnector connector)
	{
		if (connector != null)
		{
			SpellConnection connection = connector.Connection;
			
			if (connection != null)
			{
				VisitConnection(connection);
				IterateOverEffectNodes(connection.Destination.Parent);
			}
		}
	}

	public void IterateOverEffectNodes(SpellNode node)
	{
		if (effectNodeCallback != null)
		{
			effectNodeCallback(node);

			for (int i = 0; i < node.Type.InputCount; ++i)
			{
				IterateOverExpressionNodes(node.GetInputConnector(i));
			}

			for (int i = 0; i < node.Type.EventCount; ++i)
			{
				EnterTriggerEvent(node.GetEventConnector(i));
			}

			EnterTriggerEvent(node.OutConnector);
		}
	}

	private void EnterExpressionNode(SpellNode node)
	{
		if (!node.Type.IsEffect && expressionNodeCallback != null)
		{
			expressionNodeCallback(node);
			
			for (int i = 0; i < node.Type.InputCount; ++i)
			{
				IterateOverExpressionNodes(node.GetInputConnector(i));
			}
		}
	}

	public void IterateOverExpressionNodes(SpellNodeConnector connector)
	{
		SpellConnection connection = connector.Connection;
		
		if (connection != null)
		{
			VisitConnection(connection);
			EnterExpressionNode(connection.Source.Parent);
		}
	}
}
