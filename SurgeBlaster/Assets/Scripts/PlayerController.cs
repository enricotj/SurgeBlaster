using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    // prefabs
    public GameObject bulletPrefab;

    // shooting
    public float fireRate = 0.1f;
    public float chargeWindow = 0.4f;
    public float burstAngle = 45;
    public int burstNum = 5;

    private float fireTimer = 0;
    private bool isBurst = true;

    // speeds
    public float runSpeed = 12;
    public float dashSpeed = 16;
    public float bulletSpeed = 32;

    // timings
    public float dashWindow = 0.5f;
    public float dashAdjustWindow = 0.05f;
    public float trotWindow = 0.1f;

    private float dashTimer = 0;
    private float dashAdjustTimer = 0;
    private float trotTimer = 0;

    // angles
    public float dashAngle = 60;
    public float steerAngle = 90;
    public float deadZone = 0.2f;
    public float neutralZone = 0.4f;

    // interpolations
    public float steerLerp = 0.3f;
    public float pivotLerp = 0.05f;
    public float stopLerp = 0.6f;

    // components
    private Rigidbody2D rb;
    private SpriteRenderer spr;
    private Camera cam;

    // inputs
    private Vector2 moveInput = Vector2.zero;
    private Vector2 lastMoveInput = Vector2.zero;
    private Vector2 lastDash = Vector2.zero;
    private Vector2 velocity = Vector2.zero;
    private Vector3 cameraOffset = Vector3.zero;
    private bool firing = false;
    private bool hasNeutralReset;

	void Start ()
    {
        rb = GetComponent<Rigidbody2D>();
        spr = GetComponent<SpriteRenderer>();
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
	}
	
	void Update ()
    {
        GetInputs();

        float oldDash = dashTimer;

        IncrementTimers();

        if (oldDash > 0 && dashTimer == 0)
        {
            trotTimer = trotWindow;
        }

        Move();

        // DEBUG COLORS
        if (isBurst && fireTimer > 0 && firing)
        {
            spr.color = new Color(fireTimer / chargeWindow, 1, 1);
        }
        else
        {
            spr.color = Color.white;
        }

        //rb.velocity = velocity;
    }

    void FixedUpdate()
    {
        rb.velocity = velocity;
    }

    private void GetInputs()
    {
        lastMoveInput = moveInput;
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");

        if (DistanceToLine(new Ray(moveInput, lastMoveInput - moveInput), Vector3.zero) <= neutralZone)
        {
            hasNeutralReset = true;
        }
        if (moveInput.magnitude <= deadZone)
        {
            moveInput = Vector2.zero;
        }

        if (Input.GetButtonDown("Fire1"))
        {
            firing = true;
            fireTimer = chargeWindow;
        }
        if (Input.GetButtonUp("Fire1"))
        {
            firing = false;
            isBurst = true;
        }

        // adjust camera offset
        Vector3 mouseInWorld = cam.GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
        cameraOffset = transform.position + (mouseInWorld - transform.position) * 0.3f;
        cameraOffset.z = -10.0f;

        // mouse aim
        Vector2 positionOnScreen = cam.GetComponent<Camera>().WorldToViewportPoint(transform.position);
        Vector2 mouseOnScreen = (Vector2)cam.GetComponent<Camera>().ScreenToViewportPoint(Input.mousePosition);
        float angle = AngleBetweenTwoPoints(positionOnScreen, mouseOnScreen) + 180;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        // fire bullets
        if (fireTimer == 0 && firing)
        {
            if (isBurst)
            {
                isBurst = false;
                float dir = angle - burstAngle/2;
                float dirInc = burstAngle / (burstNum - 1);
                for (int i = 0; i < burstNum; i++)
                {
                    SpawnBullet(dir);
                    dir += dirInc;
                }
            }
            else
            {
                SpawnBullet(angle);
            }
            fireTimer = fireRate;
        }
    }

    public void LateUpdate()
    {
        cam.transform.position = Vector3.Lerp(cam.transform.position, cameraOffset, 0.4f);
    }

    private void Move()
    {
        bool isTrot = trotTimer > 0 && lastMoveInput == Vector2.zero;
        bool isDashAdjust = dashAdjustTimer > 0;
        bool isDashAngle = Mathf.Abs(Vector2.Angle(lastDash, moveInput)) >= dashAngle;
        bool isDash = rb.velocity.sqrMagnitude == 0 || (dashTimer > 0 && isDashAngle && hasNeutralReset);
        isDash = moveInput.sqrMagnitude != 0 && (isDash || isDashAdjust);
        if (isDash)
        {
            // starting or resetting dash
            velocity = moveInput.normalized * dashSpeed;
            lastDash = moveInput;
            hasNeutralReset = false;
            if (!isDashAdjust)
            {
                dashTimer = dashWindow;
                dashAdjustTimer = dashAdjustWindow;
            }
        }
        else if (dashTimer == 0 && rb.velocity.sqrMagnitude > 0)
        {
            bool isSteer = Vector2.Angle(rb.velocity, moveInput) <= steerAngle;
            Vector2 tv = moveInput.normalized * runSpeed;
            if (moveInput.sqrMagnitude == 0)
            {
                // stopping
                velocity = Vector2.Lerp(rb.velocity, tv, stopLerp);
            }
            else if (isSteer)
            {
                // steering
                velocity = Vector2.Lerp(rb.velocity, tv, steerLerp);
            }
            else
            {
                // pivoting
                velocity = Vector2.Lerp(rb.velocity, tv, pivotLerp);
            }
        }

        // snap to zero velocity if the magnitude is small enough
        if (moveInput.sqrMagnitude == 0 && rb.velocity.magnitude <= steerLerp)
        {
            velocity = Vector2.zero;
        }
    }

    private void IncrementTimers()
    {
        AdjustTimer(ref dashTimer);
        AdjustTimer(ref dashAdjustTimer);
        AdjustTimer(ref fireTimer);
        AdjustTimer(ref trotTimer);
    }

    private void SpawnBullet(float dir)
    {
        GameObject b = GameObject.Instantiate(bulletPrefab, transform.position, Quaternion.Euler(new Vector3(0, 0, dir)));
        Rigidbody2D brb = b.GetComponent<Rigidbody2D>();
        brb.velocity = DegreeToVector2(dir).normalized * bulletSpeed;
    }

    private void AdjustTimer(ref float timer)
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer < 0)
            {
                timer = 0;
            }
        }
    }

    private float AngleBetweenTwoPoints(Vector3 a, Vector3 b)
    {
        return Mathf.Atan2(a.y - b.y, a.x - b.x) * Mathf.Rad2Deg;
    }

    private float DistanceToLine(Ray ray, Vector3 point)
    {
        return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
    }

    private Vector2 DegreeToVector2(float degree)
    {
        return RadianToVector2(degree * Mathf.Deg2Rad);
    }

    private Vector2 RadianToVector2(float radian)
    {
        return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
    }
}
