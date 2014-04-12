
using UnityEngine;
using System.Collections;

[RequireComponent (typeof(Rigidbody))]
[RequireComponent (typeof(SphereCollider))]
public class ThirdPersonController : MonoBehaviour 
{
	public AnimationClip idleAnimation;
	public AnimationClip walkAnimation;
	public AnimationClip runAnimation;
	public AnimationClip jumpPoseAnimation;

	public float walkMaxAnimationSpeed = 0.75f;
	public float trotMaxAnimationSpeed = 1.0f;
	public float runMaxAnimationSpeed = 1.0f;
	public float jumpAnimationSpeed = 1.15f;
	public float landAnimationSpeed = 1.0f;

	private Animation _animation;

	enum CharacterState { Idle, Walking, Trotting, Running, Jumping };

	private CharacterState _characterState;

	public float walkSpeed = 2.0f;
	public float trotSpeed = 4.0f;
	public float runSpeed = 6.0f;

	public float inAirContorlAcceleration = 3.0f;

	public float jumpHeight = 0.5f;
	public float gravity = 20.0f;

	public float speedSmoothing = 10.0f;

	public float rotateSpeed = 500.0f;
	public float trotAfterSeconds = 3.0f;

	public bool canJump = true;

	private float jumpRepeatTime = 0.05f;
	private float jumpTimeout = 0.15f;
	private float groundedTimeout = 0.25f;

	private float lockCameraTimer = 0.0f;
	private Vector3 moveDirection = Vector3.zero;

	private float verticalSpeed = 0.0f;
	private float moveSpeed = 0.0f;

	private CollisionFlags collisionFlags;

	private bool jumping = false;
	private bool jumpingReachedApex = false;
	private bool movingBack = false;
	private bool isMoving = false;
	private float walkStartTime = 0.0f;

	private float lastJumpButtonTime = -10.0f;
	private float lastJumpTime = -1.0f;
	private float lastJumpStartHeight = 0.0f;

	private Vector3 inAirVelocity = Vector3.zero;

	private float lastGroundedTime = 0.0f;

	private bool isControllable = true;

	void Awake()
	{
		moveDirection = transform.TransformDirection (Vector3.forward);

		_animation = GetComponent<Animation>();
		if (!_animation)
			Debug.Log ("No animation");

		if (!idleAnimation)
			Debug.Log ("No idle anim");
		if (!walkAnimation)
			Debug.Log ("No walk anim");
		if (!runAnimation)
			Debug.Log ("No run anim");
		if (!jumpPoseAnimation && canJump) 
		{
			_animation = null; 
			Debug.Log ("No jump anim");
		}
	}

	public void UpdateSmoothedMovementDirection()
	{
		Transform cameraTransform = Camera.main.transform;
		var grounded = IsGrounded ();

		Vector3 forward = cameraTransform.TransformDirection(Vector3.forward);
		forward.y = 0;
		forward = forward.normalized;

		Vector3 right = new Vector3(forward.z, 0, -forward.x);

		float v = Input.GetAxisRaw("Vertical");
		float h = Input.GetAxisRaw("Horizontal");

		if (v < -0.2)
			movingBack = true;
		else
			movingBack = false;

		bool wasMoving = isMoving;
		isMoving = Mathf.Abs(h) > 0.1 || Mathf.Abs(v) > 0.1;

		Vector3 targetDirection = h * right + v * forward;

		if (grounded)
		{
			lockCameraTimer += Time.deltaTime;

			if (isMoving != wasMoving)
				lockCameraTimer = 0.0f;

			if (targetDirection != Vector3.zero)
			{
				if (moveSpeed < walkSpeed * 0.9 && grounded)
				{
					moveDirection = targetDirection.normalized;
				}
				else
				{
					moveDirection = Vector3.RotateTowards(moveDirection, targetDirection, rotateSpeed * Mathf.Deg2Rad * Time.deltaTime, 1000);

					moveDirection = moveDirection.normalized;
				}
			}

			float curSmooth = speedSmoothing * Time.deltaTime;

			float targetSpeed = Mathf.Min (targetDirection.magnitude, 1.0f);

			_characterState = CharacterState.Idle;

			if (Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift))
			{
				targetSpeed *= runSpeed;
				_characterState = CharacterState.Running;
			}

			else if (Time.time - trotAfterSeconds > walkStartTime)
			{
				targetSpeed *= trotSpeed;
				_characterState = CharacterState.Trotting;
			}
			else
			{
				targetSpeed *= walkSpeed;
			_characterState = CharacterState.Walking;
			}

			moveSpeed = Mathf.Lerp (moveSpeed, targetSpeed, curSmooth);

			if (moveSpeed < walkSpeed * 0.3)
			    walkStartTime = Time.time;
		}
		else
		{
			if(jumping)
				lockCameraTimer = 0.0f;

			if (isMoving)
			    inAirVelocity += targetDirection.normalized * Time.deltaTime * inAirContorlAcceleration;

		}
	}

	private void ApplyJumping()
	{
		if (lastJumpTime + jumpRepeatTime > Time.time)
			return;

		if (IsGrounded())
		{
			if (canJump && Time.time < lastJumpButtonTime + jumpTimeout)
			{
				verticalSpeed = CalculateJumpVerticalSpeed(jumpHeight);
				DidJump();
			}
		}
	}

	private void ApplyGravity()
	{
		if (isControllable)
		{
			bool jumpButton = Input.GetButton("Jump");

			if (jumping && !jumpingReachedApex && verticalSpeed <= 0.0)
			{
				jumpingReachedApex = true;
				SendMessage("DidJumpReachApex", SendMessageOptions.DontRequireReceiver);
			}

			if (IsGrounded())
				verticalSpeed = 0.0f;
			else
				verticalSpeed -= gravity * Time.deltaTime;
		}
	}

	private float CalculateJumpVerticalSpeed(float targetJumpHeight)
	{
		return Mathf.Sqrt(2 * targetJumpHeight * gravity);
	}

	private void DidJump()
	{
		jumping = true;
		jumpingReachedApex = false;
		lastJumpTime = Time.time;
		lastJumpStartHeight = transform.position.y;
		lastJumpButtonTime = -10;

		_characterState = CharacterState.Jumping;
	}

	void Update()
	{
		if (!isControllable)
			Input.ResetInputAxes();

		if (Input.GetButtonDown("Jump"))
			lastJumpButtonTime = Time.time;

		UpdateSmoothedMovementDirection();

		ApplyGravity();

		ApplyJumping();

		Vector3 movement = moveDirection * moveSpeed + new Vector3(0, verticalSpeed, 0) + inAirVelocity;
		movement *= Time.deltaTime;
		
		gameObject.transform.position += movement;

		if (_animation)
		{
			if (_characterState == CharacterState.Jumping)
			{
				if (!jumpingReachedApex)
				{
					_animation[jumpPoseAnimation.name].speed = jumpAnimationSpeed;
					_animation[jumpPoseAnimation.name].wrapMode = WrapMode.ClampForever;
					_animation.CrossFade(jumpPoseAnimation.name);
				}
				else
				{
					_animation[jumpPoseAnimation.name].speed = -landAnimationSpeed;
					_animation[jumpPoseAnimation.name].wrapMode = WrapMode.ClampForever;
					_animation.CrossFade(jumpPoseAnimation.name);
				}
			}
		}
		else
		{
			if (_characterState == CharacterState.Running)
			{
				_animation[runAnimation.name].speed = Mathf.Clamp(gameObject.rigidbody.velocity.magnitude, 0.0f, runMaxAnimationSpeed);
				_animation.CrossFade(runAnimation.name);
			}
			else if (_characterState == CharacterState.Trotting)
			{
				_animation[walkAnimation.name].speed = Mathf.Clamp(gameObject.rigidbody.velocity.magnitude, 0.0f, trotMaxAnimationSpeed);
				_animation.CrossFade(walkAnimation.name);
			}
			else if (_characterState == CharacterState.Walking)
			{
				_animation[walkAnimation.name].speed = Mathf.Clamp(gameObject.rigidbody.velocity.magnitude, 0.0f, walkMaxAnimationSpeed);
				_animation.CrossFade(walkAnimation.name);
			}
		}

		if (IsGrounded())
		{
			transform.rotation = Quaternion.LookRotation(moveDirection);
		}
		else
		{
			Vector3 xzMove = movement;
			xzMove.y = 0;
			if(xzMove.sqrMagnitude > 0.001);
			{
				transform.rotation = Quaternion.LookRotation(xzMove);
			}
		}

		if (IsGrounded())
		{
			lastGroundedTime = Time.time;
			inAirVelocity = Vector3.zero;
			if(jumping)
			{
				jumping = false;
				SendMessage("DidLand", SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	public float GetSpeed()
	{
		return moveSpeed;
	}

	public bool IsJumping()
	{
		return jumping;
	}

	public bool IsGrounded()
	{
		RaycastHit hit = new RaycastHit();
		Vector3 dir = new Vector3(0, -1, 0);
		float dist = 10;

		Physics.Raycast(transform.position, dir, out hit, dist);

		Debug.DrawRay(transform.position, dir * dist, Color.green);

		if (hit.collider.tag == "Ground")
			return true;
		else return false;
	}

	public Vector3 GetDirection()
	{
		return moveDirection;
	}

	public bool IsMovingBackwards()
	{
		return movingBack;
	}

	public float GetLockCameraTimer()
	{
		return lockCameraTimer;
	}

	public bool IsMoving()
	{
		return Mathf.Abs(Input.GetAxisRaw("Vertical")) + Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.5;
	}

	public bool HasJumpReachedApex()
	{
		return jumpingReachedApex;
	}

	public bool IsGroundedWithTimeout()
	{
		return lastGroundedTime + groundedTimeout > Time.time;
	}

	public void Reset()
	{
		gameObject.tag = "Player";
	}
}
