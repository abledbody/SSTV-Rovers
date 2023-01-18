using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class Rover : MonoBehaviour {
	public Transform cam;
	Rigidbody rb;
	PlayerInput playerInput;
	SSTVCamera sstvCamera;
	
	public RoverParameters parameters;

	float angle;
	bool mouseLock;


	void Awake() {
		rb = GetComponent<Rigidbody>();
		playerInput = GetComponent<PlayerInput>();
		sstvCamera = GetComponentInChildren<SSTVCamera>();
	}

	void FixedUpdate() {
		var gravityNormal = Physics.gravity.normalized;
		if (Physics.Raycast(transform.position, gravityNormal, out var hit, parameters.chassisClearance)) {
			ApplyControls(gravityNormal);
			Correct(gravityNormal, hit);
		}
	}

	public void CaptureInput(InputAction.CallbackContext context) {
		if (mouseLock && context.performed) {
			sstvCamera.Capture();
		}
	}

	void ApplyControls(Vector3 gravityNormal) {
		var move = playerInput.actions["Move"].ReadValue<Vector2>();
		var look = mouseLock ? playerInput.actions["Look"].ReadValue<Vector2>() : Vector2.zero;
		
		var forwardVel = Vector3.Dot(rb.velocity, transform.forward);
		forwardVel = Mathf.MoveTowards(
			forwardVel,
			move.y * parameters.maxSpeed,
			parameters.acceleration * Time.fixedDeltaTime
		);
		// Set the velocity along the local z axis to forwardVel, set the velocity along the tangent to gravity to 0,
		// and preserve the velocity along the gravity normal.
		rb.velocity = transform.forward * forwardVel + gravityNormal * Vector3.Dot(rb.velocity, gravityNormal);

		rb.angularVelocity = transform.up * Mathf.Clamp(
			move.x * parameters.maxTurnSpeed + look.x * parameters.lookSpeed,
			-parameters.maxTurnSpeed,
			parameters.maxTurnSpeed
		);
		angle = Mathf.Clamp(
			angle - look.y * parameters.lookSpeed * Mathf.Rad2Deg * Time.fixedDeltaTime,
			parameters.minLookAngle,
			parameters.maxLookAngle
		);

		cam.localRotation = Quaternion.Euler(angle, 0, 0);
	}

	void Correct(Vector3 gravityNormal, RaycastHit hit) {
		// Modify the velocity along the gravity normal so that next frame the rover will be chassisClearance above the ground.
		// Step 1: Remove the velocity along the gravity normal.
		var velocity = rb.velocity - gravityNormal * Vector3.Dot(rb.velocity, gravityNormal);
		// Step 2: Add the velocity needed to move the rover chassisClearance above the ground.
		rb.velocity = velocity - gravityNormal * (parameters.chassisClearance - hit.distance) / Time.fixedDeltaTime;

		print($"Clearance: {hit.distance}, Velocity on gravityNormal: {Vector3.Dot(rb.velocity, gravityNormal)}");
	}

	public void ToggleMouseLock(InputAction.CallbackContext context) {
		if (context.performed) {
			mouseLock = !mouseLock;
			Cursor.lockState = mouseLock ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !mouseLock;
		}
	}
}

[System.Serializable]
public struct RoverParameters {
	public float maxSpeed;
	public float acceleration;
	public float maxTurnSpeed;
	public float turnAcceleration;
	public float lookSpeed;
	public float minLookAngle;
	public float maxLookAngle;
	public float chassisClearance;


	public RoverParameters(
		float maxSpeed,
		float acceleration,
		float maxTurnSpeed,
		float turnAcceleration,
		float lookSpeed,
		float minLookAngle,
		float maxLookAngle,
		float chassisClearance
	) {
		this.maxSpeed = maxSpeed;
		this.acceleration = acceleration;
		this.maxTurnSpeed = maxTurnSpeed;
		this.turnAcceleration = turnAcceleration;
		this.lookSpeed = lookSpeed;
		this.minLookAngle = minLookAngle;
		this.maxLookAngle = maxLookAngle;
		this.chassisClearance = chassisClearance;
	}
}