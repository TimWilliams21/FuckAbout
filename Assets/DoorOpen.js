#pragma strict

function Start () {

}

function Update () {
if(Input.GetKeyDown("o"))
	gameObject.transform.Rotate(Vector3(0,0,30));
}