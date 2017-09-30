using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoclipCamera : MonoBehaviour
{
	const float moveSpeed = 10f;
	const float rotationSpeed = 200f;
	
	// Update is called once per frame
	void Update ()
	{
		Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		Vector2 rotationInput = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));

		Vector3 moveDelta = Vector3.forward * moveInput.y + Vector3.right * moveInput.x;
		moveDelta *= moveSpeed * Time.deltaTime;
		transform.Translate(moveDelta);

		if (Input.GetMouseButton(0))
		{
			transform.Rotate(Vector3.up * rotationInput.x * rotationSpeed * Time.deltaTime);
			transform.Rotate(transform.right, rotationInput.y * rotationSpeed * Time.deltaTime);
			Vector3 eulers = transform.eulerAngles;
			eulers.z = 0;
			transform.eulerAngles = eulers;
		}
	}
}
