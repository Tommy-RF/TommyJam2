// Handles physics for each player-ball.
// Checks for collisions with other players and the "Ready" trigger area.

using PurrNet;
using PurrNet.Transports;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using static Unity.VisualScripting.Member;

public class PlayerPhysics : NetworkBehaviour
{
    //[SerializeField] private float moveForce = 10f;
    //[SerializeField] private float jumpForce = 10f;
    [SerializeField] private float bounceForce = 10f;
    [SerializeField] private Rigidbody rigidbody;
    public AudioSource PlayerAudio;
    //public float maxPower = 10f;
    //private bool isDragging = false;
    //private Vector3 startPoint;

    private PlayerNetwork playerNetwork;

    public Material P1;
    public Material P2;
    public Material P3;
    public Material P4;

    public Material P1T;
    public Material P2T;
    public Material P3T;
    public Material P4T;


    public GameObject launchForceText;

    public int PlayerNumber;


    public float launchForce;
    public float minLaunchForce;
    public float maxLaunchForce;
    public float smoothSpeed = 5f;
    public float currentValue;
    public float targetValue;
    public float waitForce;
    public bool isLaunching;
    public bool canLaunch;
    public Vector3 targetPos;
    public TrailRenderer trail;
    public GameObject particles;
    public bool isPaused;

    protected override void OnSpawned(bool asServer)
    {
        playerNetwork = GetComponent<PlayerNetwork>();
        base.OnSpawned(asServer);
        if (asServer)
            return;

        //All clients set it to kinematic, so only the server runs physics!
        rigidbody.isKinematic = !isServer;
        //Only the owner has it enabled, as to run Update()
        enabled = isOwner;

        //Only the owner runs OnTick to send input to the server
        if (isOwner)
        {
            networkManager.onTick += OnTick;
        }
        if (!isOwner)
        {
            launchForceText.SetActive(false);
        }

        currentValue = minLaunchForce;
        targetValue = minLaunchForce;



        var colors = new[] { P1, P2, P3, P4 };
        var Tcolors = new[] { P1T, P2T, P3T, P4T };
        int index = (int)(owner.Value.id % (ulong)colors.Length - 1);
        GetComponentInChildren<MeshRenderer>().material = colors[index];
        trail.material = Tcolors[index];
        PlayerNumber = index +1;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        //Unsubcribing again for cleanup
        networkManager.onTick -= OnTick;
    }


    private void Update()
    {
        //We have to store the input to be used during the next tick
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, 0f); // Adjust height as needed

        //this.rigidbody.linearVelocity.sqrMagnitude < waitForce

        if (Input.GetMouseButton(0) && canLaunch)
        {
            if (isOwner)
            {
                launchForceText.SetActive(true);
            }
            //launchForceText.transform.LookAt(Camera.main.transform);
            targetValue = maxLaunchForce;
            particles.SetActive(true);
        }
        else
        {
            launchForceText.SetActive(false);
            targetValue = minLaunchForce;
            particles.SetActive(false);
        }

        if (this.transform.position.x > 17f || this.transform.position.x < -17f || this.transform.position.z > 9 || this.transform.position.z < -9 || this.transform.position.y < -1f)
        {
            this.transform.position = new Vector3(0, 0.5f, 0);
        }



        currentValue = Mathf.Lerp(currentValue, targetValue, smoothSpeed * Time.deltaTime);
        launchForceText.GetComponent<TMP_Text>().SetText(currentValue.ToString("F0"));

        particles.GetComponent<ParticleSystem>().startSpeed = currentValue/50;
        particles.GetComponent<ParticleSystem>().emissionRate = currentValue/5;

        //float hitdist = 0.0f;
        //if (groundPlane.Raycast(ray, out hitdist))
        //{
        //    Vector3 targetPoint = ray.GetPoint(hitdist);
        //    Quaternion targetRotation = Quaternion.LookRotation(targetPoint - transform.position);
        //    particles.transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 100 * Time.deltaTime);

        //}

        if (groundPlane.Raycast(ray, out float enter) && Input.GetMouseButtonUp(0) && this.rigidbody.linearVelocity.sqrMagnitude < waitForce)
        {
            targetPos = ray.GetPoint(enter);
            isLaunching = true;
            launchForce = currentValue;
            StartCoroutine(CountdownToLaunch());

        }

        //if (Input.GetKeyDown(KeyCode.M))
        //{
        //    if (isPaused == false)
        //    {
        //        isPaused = true;
        //        PlayerAudio.volume = 0;
        //    }
        //    else if (isPaused == true)
        //    {
        //        isPaused = false;
        //        PlayerAudio.volume = 0.5f;
        //    }
        //}

    }

    public IEnumerator CountdownToLaunch()
    {
        canLaunch = false;

        // Pause execution for 3 seconds
        yield return new WaitForSeconds(3f);

        canLaunch = true;
    }

    private void FixedUpdate()
    {
        //if (this.rigidbody.linearVelocity.sqrMagnitude < waitForce)
        //{
        //    canLaunch = true;
        //    Debug.Log($"{rigidbody.linearVelocity.sqrMagnitude}");
        //}
        //else
        //{
        //    canLaunch = false;
        //}
        //playerNetwork.ballPosition = (int) transform.position.x;
    }

    private void OnTick(bool asServer)
    {
        //In case of a host setup, we don't want this to run twice.
        if (asServer)
            return;

        if (isLaunching)
        {
            LaunchBall(targetPos, launchForce);
            isLaunching = false;
        }

        ////We generate the input struct that will be sent to the server
        //var input = new InputData()
        //{
        //    movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")),
        //};

        ////We send the input to the server
        //Move(input);
    }

    [ServerRpc(Channel.Unreliable)]
    private void LaunchBall(Vector3 target, float launchSpeed)
    {
        if (this.rigidbody.linearVelocity.sqrMagnitude > waitForce)
        {
            Debug.Log("Already Launched!");
            return;
        }
        Debug.Log("Launching!!!");
        Vector3 direction = (target - transform.position).normalized;
        GetComponent<Rigidbody>().AddForce(direction * launchSpeed, ForceMode.Impulse);

    }

    //Server RPC with Unreliable channel to send the data more efficiently
    [ServerRpc(Channel.Unreliable)]
    private void Move(InputData inputData)
    {


        //This is where you can also handle cheat detection on the inputData
        //You could for example normalize it, if the magnitude is above 1
        //From here the code is basically "single-player" code from the 
        //perspective of the server

        //We generate the movement vector from the given input.
        //var movement = new Vector3(inputData.movement.x, 0, inputData.movement.y) * moveForce;

        //rigidbody.AddForce(movement);
        //if (Input.GetMouseButtonDown(0))
        //{
        //    isDragging = true;
        //    // Convert screen mouse position to world position
        //    startPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //    startPoint.y = 0; // Adjust Z based on 2D/3D setup
        //}

        //if (Input.GetMouseButton(0) && isDragging)
        //{
        //    Vector3 currentPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //    currentPoint.y = 0;

        //    // Calculate drag vector
        //    Vector3 dragVector = currentPoint - startPoint;

        //    // Clamp magnitude to max power
        //    Vector3 clampedForce = Vector2.ClampMagnitude(dragVector, maxPower);

        //    // Update UI power meter
        //    //powerSlider.value = clampedForce.magnitude / maxPower;
        //}

        //if (Input.GetMouseButtonUp(0))
        //{
        //    if (isDragging)
        //    {
        //        Vector3 currentPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //        currentPoint.y = 0;
        //        Vector3 dragVector = currentPoint - startPoint;
        //        Vector3 finalForce = Vector2.ClampMagnitude(dragVector, maxPower);

        //        // Launch ball
        //        rigidbody.AddForce(finalForce, ForceMode.Impulse);
        //        isDragging = false;
        //        //powerSlider.value = 0; // Reset UI
        //    }
        //}
    }

    private void OnCollisionEnter(Collision other)
    {
        //Other than the if-statement here, this is single-player code from the
        //perspective of the server
        if (!isServer)
            return;

        if (!other.gameObject.TryGetComponent(out PlayerPhysics otherPlayer))
            return;

        var direction = (transform.position - other.transform.position).normalized;
        rigidbody.AddForce(direction * bounceForce, ForceMode.Impulse);



    }

    // The "Ready" trigger area is the square in the middle of the map. When a player enters it, they are marked as ready. When they leave, they are marked as not ready.
    // This is used to determine when all players are ready to start the game. The GameManager script checks the isReady variable of each player to determine if the game can start.
    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            float randomInt = Random.Range(0.5f, 1.5f);
            PlayerAudio.pitch = randomInt;
            PlayerAudio.Play();
        }

        if (other.CompareTag("Ready"))
        {
            // Handle collection
            playerNetwork.isReady = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ready"))
        {
            // Handle collection
            playerNetwork.isReady = false;
        }
    }

    //Struct in which we hold input data. This isn't necessary, just a clean approach
    private struct InputData
    {
        public Vector3 movement;
    }
}
