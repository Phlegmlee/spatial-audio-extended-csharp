using Godot;

public partial class PlayerEntity : CharacterBody3D
{
	[Export] Camera3D playerCamera;
	private Vector2 MouseInput = new();
	private float _mouseSens = 0.1f;

	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Switch Mouse Mode
		if (Input.IsActionJustPressed("mouseMode"))
		{
			switch (Input.MouseMode)
			{
				case Input.MouseModeEnum.Visible:
					Input.MouseMode = Input.MouseModeEnum.Captured;
					break;
				case Input.MouseModeEnum.Captured:
					Input.MouseMode = Input.MouseModeEnum.Visible;
					break;
			}
		}

		HandleCameraRotation();

		Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
		inputDir = inputDir.Rotated(-playerCamera.Rotation.Y);
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if
		(
			@event is InputEventMouseMotion mouseMotion
			&& Input.MouseMode == Input.MouseModeEnum.Captured
		)
		{
			MouseInput = mouseMotion.Relative;
		}
	}

	private void HandleCameraRotation()
	{
		Vector3 camRot = playerCamera.RotationDegrees;
		camRot.Y -= MouseInput.X * _mouseSens;
		camRot.X -= MouseInput.Y * _mouseSens;
		playerCamera.RotationDegrees = camRot;

		MouseInput = Vector2.Zero;
	}
}
