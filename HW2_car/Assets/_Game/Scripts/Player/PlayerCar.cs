using UnityEngine;
using UnityEngine.InputSystem;
namespace NeonHighway
{
    public sealed class PlayerCar : MonoBehaviour
    {
        public GameConfig config;
        public InputActionAsset input;
        public Transform visual;
        public Camera followCamera;
        public Light underglow;
        public Renderer neonRenderer;
        public ParticleSystem exhaust;
        public Renderer leftIndicator, rightIndicator;
        InputAction left,right,pause;
        int lane=1;
        int signal;
        float signalTime;
        float xVelocity,shake;
        Vector3 camVelocity;
        Material neon;
        Rigidbody body;
        void Awake()
        {
            body=GetComponent<Rigidbody>();
            input=Instantiate(input);
            left=input.FindAction("Driving/Left",true);
            right=input.FindAction("Driving/Right",true);
            pause=input.FindAction("Driving/Pause",true);
            neon=neonRenderer.material;
            underglow.color=neon.GetColor("_BaseColor");
        }
        void OnEnable()
        {
            input.Enable();
            left.performed+=MoveLeft;
            right.performed+=MoveRight;
            pause.performed+=Pause;
        }
        void OnDisable()
        {
            left.performed-=MoveLeft;
            right.performed-=MoveRight;
            pause.performed-=Pause;
            input.Disable();
        }
        void MoveLeft(InputAction.CallbackContext ctx)
        {
            ChangeLane(-1);
        }
        void MoveRight(InputAction.CallbackContext ctx)
        {
            ChangeLane(1);
        }
        void ChangeLane(int direction)
        {
            if (!GameManager.Instance || !GameManager.Instance.IsPlaying)
                return;

            int nextLane = Mathf.Clamp(lane + direction, 0, 2);
            if (nextLane == lane)
                return;

            lane = nextLane;
            signal = direction;
            signalTime = .65f;
        }
        void Pause(InputAction.CallbackContext ctx)
        {
            if (GameManager.Instance)
                GameManager.Instance.TogglePause();
        }
        void FixedUpdate()
        {
            var gm=GameManager.Instance;
            if(gm==null||!gm.IsPlaying)return;
            var position=body.position;
            position.x=Mathf.SmoothDamp(position.x,(lane-1)*config.laneWidth,ref xVelocity,.11f,Mathf.Infinity,Time.fixedDeltaTime);
            position.z+=gm.Speed*Time.fixedDeltaTime;
            body.MovePosition(position);
        }
        public void ShiftOrigin(float distance)
        {
            body.position-=Vector3.forward*distance;
            transform.position=body.position;
            ResetCamera();
        }
        void Update()
        {
            var gm=GameManager.Instance;
            if(gm==null)return;
            if(gm.IsPlaying)
            {
                visual.localRotation=Quaternion.Slerp(visual.localRotation,Quaternion.Euler(0,xVelocity*.9f,-xVelocity*.85f),Time.deltaTime*9);
            }
            if (gm.IsPlaying)
                signalTime -= Time.deltaTime;
            bool flashing = gm.IsPlaying && signalTime > 0 && Mathf.Repeat(signalTime, .32f) > .11f;
            if (leftIndicator) leftIndicator.enabled = flashing && signal < 0;
            if (rightIndicator) rightIndicator.enabled = flashing && signal > 0;
            float t=Mathf.InverseLerp(config.initialSpeed,config.maximumSpeed,gm.Speed);
            underglow.intensity=Mathf.Lerp(3,10,t);
            neon.SetColor("_EmissionColor",underglow.color*Mathf.Lerp(3,9,t));
            var emission=exhaust.emission;
            emission.enabled=gm.IsPlaying;
            visual.localScale=Vector3.one*(gm.Invulnerable?(Mathf.Sin(Time.time*35)>0?1:.96f):1);
        }
        void LateUpdate()
        {
            var gm=GameManager.Instance;
            if(gm==null||gm.State==GameState.Paused)return;
            Vector3 desired=transform.position+new Vector3(-transform.position.x*.62f,5.8f,-10.8f);
            followCamera.transform.position=Vector3.SmoothDamp(followCamera.transform.position,desired,ref camVelocity,.13f);
            shake=Mathf.Max(0,shake-Time.deltaTime*2);
            Vector3 aim = transform.position + new Vector3(0,1.1f,18);
            followCamera.transform.rotation=Quaternion.Slerp(followCamera.transform.rotation,Quaternion.LookRotation(aim-followCamera.transform.position),Time.deltaTime*7);
            if (shake > 0) followCamera.transform.position += Random.insideUnitSphere * shake * .18f;
            followCamera.fieldOfView=Mathf.Lerp(61,72,Mathf.InverseLerp(24,58,gm.Speed));
        }
        public void Shake()=>shake=1;
        public void ResetCamera()
        {
            followCamera.transform.position=transform.position+new Vector3(0,5.8f,-10.8f);
            camVelocity=Vector3.zero;
        }
        void OnDestroy()
        {
            if(!Application.isPlaying)return;
            if(neon!=null)Destroy(neon);
            if(input!=null)Destroy(input);
        }
    }
}
