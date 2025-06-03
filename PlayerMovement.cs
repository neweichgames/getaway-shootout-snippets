using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour {

	public KeyCode[] jumpKeys;
	public string[] touchButtons;
	public AudioSource jumpSound;

	private Player player;
	private Rigidbody2D r;

	private bool movementEnabled = true;
	private bool upRight;
	private bool turning;
	private bool turnRight = true;
	private Transform armPivot;

	//Rotate
	[Header("Rotation Variables")]
	public float maxJumpPower = 350;
	public float maxJumpTurn = 0.5f;
	public float turnSpeed;
	private float lerpTurnSpeed;
	private float turnUprightSpeed = 1.35f;
	private float turnSensitivity = 40f;
	private float timeRotating = 0f;
	private float dampRotationZone = 5.75f;

	//Jump
	[Header("Jumping Variables")]
	private float jumpTime = 0.15f;
	private float jumpMaxVelocity = 4f;

    void Start(){
		player = GetComponent<Player>();
		r = GetComponent<Rigidbody2D> ();

		armPivot = transform.GetChild (0).Find ("ArmPivot");

		if (player != null && player.playerID < Settings.realPlayers.GetValue()) {
			turnSensitivity += (Settings.turnSensitivity.GetValue() - 2) * 10f;
		}
    }

	void Update () {
		if (jumpTime >= 0)
			jumpTime -= Time.deltaTime;

        if (movementEnabled)
        {
			// Damp angular velocity when character is in upright position
			DampAngularVel();

			//Check keys if they are down
			if (player != null && player.IsRealPlayer())
				CheckInput();
		}
	}

    private void FixedUpdate()
    {
		if (movementEnabled && turning)
			UpdateTurn();
	}

    void DampAngularVel()
    {
		float rot = GetRotation();
		if (rot < dampRotationZone && rot > -dampRotationZone)
		{
			if (upRight == false)
			{
				r.angularVelocity = r.angularVelocity / 100;
				upRight = true;
			}
		}
	}

	void CheckInput()
    {
		// If we are not turning check button down for starting turn
		if (turning == false)
		{
			if (jumpTime < 0)
            {
				for (int i = 0; i < jumpKeys.Length; i++)
				{
					if ((Input.GetKey(jumpKeys[i]) || TouchManager.Instance.IsButtonDown(touchButtons[i])) && isGrounded())
					{
						StartTurn(i == 1);
						return;
					}
				}
			}
        }
		// If turning Check for button up to jump
		else
		{
			int key = turnRight ? 1 : 0;
			if (Input.GetKeyUp(jumpKeys[key]) || TouchManager.Instance.GetButtonUp(touchButtons[key]))
				Jump();
		}	
	}

	public void StartTurn(bool turnDir){
		r.constraints = RigidbodyConstraints2D.FreezeRotation;

		// Flip character to turn direction
		if(turnRight != turnDir)
        {
			transform.GetChild(0).localScale = new Vector3(turnDir ? -1 : 1, 1, 1);
			armPivot.eulerAngles = new Vector3(0, 0, armPivot.eulerAngles.z * -1f);
		}

		turning = true;
		turnRight = turnDir;
	}

	void UpdateTurn()
    {
		if (Time.timeScale == 0)
			return;

		// Replaced old way of increasing lerp turn speed due to frame dependent speed
		lerpTurnSpeed = Mathf.Min(turnSpeed, lerpTurnSpeed + Time.fixedDeltaTime * turnSpeed / 0.5f);

		int turnMult = (turnRight ? -1 : 1);

        float rot = Mathf.Repeat(r.rotation, 360f);
		if (rot > 180f)
			rot -= 360f;

		if ((rot >= maxJumpTurn * turnMult) == turnRight)
			r.SetRotation(rot + lerpTurnSpeed * Time.fixedDeltaTime * turnSensitivity * turnMult);
	}

	public void Jump(){
		//Computer check
		if(jumpTime > 0){
			Debug.Log ("Jump is still waiting " + jumpTime);
		}

		//if (r.velocity.y > jumpMaxVelocity) {
		//	Debug.Log ("Too fast jump!");
		//}

		if (isGrounded () && jumpTime < 0 && r.velocity.y < jumpMaxVelocity) {
			r.AddForce (transform.up * maxJumpPower / 50, ForceMode2D.Impulse);
		}
			
		jumpTime = 0.15f;
		lerpTurnSpeed = 0;
		r.constraints = RigidbodyConstraints2D.None;

		turning = false;

		jumpSound.Play ();
	}

	public bool isGrounded(){
		// Currently avoid using transform.position for raycasts since we are using rigidbody interpolation so the transform postion
		// is not exactly the rigidbody position and we want to avoid raycasting inside the character
		Vector2 rigidDown = new Vector2(Mathf.Sin(r.rotation * Mathf.Deg2Rad), -Mathf.Cos(r.rotation * Mathf.Deg2Rad));
		Vector2 rigidRight = new Vector2(-rigidDown.y, rigidDown.x);
		Vector2 raycastPos = r.position + rigidDown * 0.01f;

		for (int i = 0; i < 3; i++) {
			RaycastHit2D rc = Physics2D.Raycast(raycastPos + rigidRight * (i - 1) * 0.275f, rigidDown, 0.2f);

			if (rc != false) {
				return true;
			}
		}
		return false;
	}

	void OnCollisionStay2D(Collision2D hit){
		if (this.enabled == false || (player != null && player.IsDead()) || turning)
			return;

		RotateUpright();
	}

	void RotateUpright()
    {
		float rot = GetRotation();

		rot = rot / 180;

		//ROTATE
		if (rot > 0.04f || rot < -0.04f)
		{
			timeRotating += Time.deltaTime;
			float extraForceMult = Mathf.Clamp(Mathf.Ceil(timeRotating / 1.2f), 1f, 4f);
			float neededRotation = Mathf.Clamp(-rot * 40, -20, 20) * extraForceMult;
			r.AddTorque(neededRotation * turnUprightSpeed);
			upRight = false;
		}
		else
			timeRotating = 0;
	}

	public void EnableMovement(bool a)
    {
		if(a == false)
			turning = false;

		movementEnabled = a;
    }

	float GetRotation()
	{
		// TODO: Check to see if euler angles is robust enough for getting z rotation
		// If not could potentially use the calcluation below
		// float r = Vector2.SignedAngle(Vector2.up, transform.up);

		float r = transform.eulerAngles.z;
		if (r > 180)
			r -= 360;

		return r;
	}

	public void SetDirection(bool right)
	{
		turnRight = right;
        transform.Find("PlayerGraphics").localScale = new Vector3(right ? -1 : 1, 1, 1);
    }
}