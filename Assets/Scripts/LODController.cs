using UnityEngine;
using System.Collections;

public class LODController : MonoBehaviour 
{
	public Mesh HighRes;
	public Mesh LowRes;

	public Material HighResMat;
	public Material LowResMat;

	public Transform Player;

	public MeshFilter thisMeshFilter;

	public bool isHighRes = false;

	public float Distance;

	// Use this for initialization
	void Start () 
	{
		thisMeshFilter.mesh = LowRes;
		gameObject.renderer.material = LowResMat;
	}
	
	// Update is called once per frame
	void Update () 
	{
		float tempDistance = (gameObject.transform.position - Player.position).magnitude;

		Debug.Log (tempDistance + " " + isHighRes);

		if (tempDistance < Distance && !isHighRes) 
		{
			thisMeshFilter.mesh = HighRes;
			gameObject.renderer.material = HighResMat;
			isHighRes = true;
		} 
		else if (tempDistance >= Distance && isHighRes)
		{
			thisMeshFilter.mesh = LowRes;
			gameObject.renderer.material = LowResMat;
			isHighRes = false;
		}
	}
}
