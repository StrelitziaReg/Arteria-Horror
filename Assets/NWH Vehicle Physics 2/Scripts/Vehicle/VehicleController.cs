using System.Collections.Generic;
using NWH.VehiclePhysics2.Effects;
using NWH.VehiclePhysics2.Modules;
using NWH.VehiclePhysics2.Modules.Trailer;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using NWH.VehiclePhysics2.Sound;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Main class controlling all the other parts of the vehicle.
    /// </summary>
    public class VehicleController : Vehicle
    {
        public const string defaultResourcesPath = resourcesPath + "Defaults/";
        public const string resourcesPath = "NWH Vehicle Physics/";
        
        public Brakes brakes = new Brakes();
        public DamageHandler damageHandler = new DamageHandler();
        public EffectManager effectsManager = new EffectManager();
        public GroundDetection.GroundDetection groundDetection = new GroundDetection.GroundDetection();
        public Input.Input input = new Input.Input();
        public ModuleManager moduleManager = new ModuleManager();
        public Powertrain.Powertrain powertrain = new Powertrain.Powertrain();
        public SoundManager soundManager = new SoundManager();
        public Steering steering = new Steering();

        /// <summary>
        /// Position of the engine relative to the vehicle. Turn on gizmos to see the marker.
        /// </summary>
        [UnityEngine.Tooltip("Position of the engine relative to the vehicle. Turn on gizmos to see the marker.")]
        public Vector3 enginePosition = new Vector3(0f, 0.4f, 1.5f);
        
        /// <summary>
        /// Position of the exhaust relative to the vehicle. Turn on gizmos to see the marker.
        /// </summary>
        [UnityEngine.Tooltip("Position of the exhaust relative to the vehicle. Turn on gizmos to see the marker.")]
        public Vector3 exhaustPosition = new Vector3(0f, 0.1f, -2f);

        /// <summary>
        ///     Used as a threshold value for lateral slip. When absolute lateral slip of a wheel is
        ///     lower than this value wheel is considered to have no lateral slip (wheel skid). Used mostly for effects and sound.
        /// </summary>
        [Tooltip(
            "Used as a threshold value for lateral slip. When absolute lateral slip of a wheel is\r\nlower than this value wheel is considered to have no lateral slip (wheel skid). Used mostly for effects and sound.")]
        public float lateralSlipThreshold = 0.2f;

        /// <summary>
        ///     Used as a threshold value for longitudinal slip. When absolute longitudinal slip of a wheel is
        ///     lower than this value wheel is considered to have no longitudinal slip (wheel spin). Used mostly for effects and
        ///     sound.
        /// </summary>
        [Tooltip(
            "Used as a threshold value for longitudinal slip. When absolute longitudinal slip of a wheel is\r\nlower than this value wheel is considered to have no longitudinal slip (wheel spin). Used mostly for effects and sound.")]
        public float longitudinalSlipThreshold = 0.4f;

        /// <summary>
        /// State settings for the current vehicle.
        /// State settings determine which components are enabled or disabled, as well as which LOD they belong to.
        /// </summary>
        [UnityEngine.Tooltip("State settings for the current vehicle.\r\nState settings determine which components are enabled or disabled, as well as which LOD they belong to.")]
        public StateSettings stateSettings;
        
        /// <summary>
        /// Position of the transmission relative to the vehicle. Turn on gizmos to see the marker.
        /// </summary>
        [UnityEngine.Tooltip("Position of the transmission relative to the vehicle. Turn on gizmos to see the marker.")]
        public Vector3 transmissionPosition = new Vector3(0f, 0.2f, 0.2f);

        /// <summary>
        ///     Prevents creeping when velocity is ~0 and the vehicle is on the slope by increasing linear drag when asleep.
        ///     To fully prevent vehicle from moving and reacting to the outer forces, use 'constrainWhenAsleep' option.
        /// </summary>
        [UnityEngine.Tooltip("    Prevents creeping when velocity is ~0 and the vehicle is on the slope by increasing linear drag when asleep.\r\n    To fully prevent vehicle from moving and reacting to the outer forces, use 'constrainWhenAsleep' option.")]
        public bool freezeWhenAsleep = true;

        /// <summary>
        /// Constrains vehicle rigidbody position and rotation so that the vehicle is fully immobile when sleeping.
        /// </summary>
        [UnityEngine.Tooltip("Constrains vehicle rigidbody position and rotation so that the vehicle is fully immobile when sleeping.")]
        public bool constrainWhenAsleep = false;
        
        
        // ************************
        // ** Physical properties
        // ************************
        
        /// <summary>
        ///     Mass of the vehicle in [kg].
        /// </summary>
        [Tooltip("    Mass of the vehicle in [kg].")]
        public float mass = 1400f;

        /// <summary>
        ///     Maximum angular velocity of the rigidbody. Use to prevent vehicle spinning unrealistically fast on collisions.
        ///     Can also be used to artificially limit tank's rotation speed.
        /// </summary>
        [Tooltip(
            "Maximum angular velocity of the rigidbody. Use to prevent vehicle spinning unrealistically fast on collisions.\r\nCan also be used to artificially limit tank's rotation speed.")]
        public float maxAngularVelocity = 8f;
        
        /// <summary>
        ///     Drag of the vehicle rigidbody.
        /// </summary>
        [Tooltip("    Drag of the vehicle rigidbody.")]
        public float drag;
        
        /// <summary>
        ///     Angular drag of the vehicle rigidbody.
        /// </summary>
        [Tooltip("    Angular drag of the vehicle rigidbody.")]
        public float angularDrag;
        
        /// <summary>
        ///     Material that will be used on all vehicle colliders
        /// </summary>
        [Tooltip("    Material that will be used on all vehicle colliders")]
        public PhysicMaterial physicsMaterial;
        
        /// <summary>
        ///     Center of mass of the rigidbody. Needs to be readjusted when new colliders are added.
        /// </summary>
        [Tooltip(
            "Center of mass of the rigidbody. Needs to be readjusted when new colliders are added.")]
        public Vector3 centerOfMass = Vector3.zero;
        
        /// <summary>
        ///     Vector by which the inertia tensor of the rigidbody will be scaled on Start().
        ///     Due to the uniform density of the rigidbodies, versus the very non-uniform density of a vehicle, inertia can feel
        ///     off.
        ///     Use this to adjust inertia tensor values.
        /// </summary>
        [Tooltip(
            "    Vector by which the inertia tensor of the rigidbody will be scaled on Start().\r\n    Due to the unform density of the rigidbodies, versus the very non-uniform density of a vehicle, inertia can feel\r\n    off.\r\n    Use this to adjust inertia tensor values.")]
        public Vector3 inertiaTensor = new Vector3(170f, 1640f, 1350f);
        
        
        /// <summary>
        ///     Vehicle dimensions in [m]. X - width, Y - height, Z - length.
        /// </summary>
        [Tooltip("    Vehicle dimensions in [m]. X - width, Y - height, Z - length.")]
        public Vector3 vehicleDimensions = new Vector3(1.5f, 1.5f, 4.6f);

        
        
        // ************************
        // ** LODs
        // ************************
        
        /// <summary>
        /// Distance between camera and vehicle used for determining LOD.
        /// </summary>
        [UnityEngine.Tooltip("Distance between camera and vehicle used for determining LOD.")]
        public float vehicleToCamDistance;
        
        /// <summary>
        /// Currently active LOD.
        /// </summary>
        [UnityEngine.Tooltip("Currently active LOD.")]
        public LOD activeLOD;
        
        /// <summary>
        /// Currently active LOD index.
        /// </summary>
        [UnityEngine.Tooltip("Currently active LOD index.")]
        public int activeLODIndex;

        /// <summary>
        /// LODs will only be updated when this value is true.
        /// Does not affect sleep LOD.
        /// </summary>
        [UnityEngine.Tooltip("LODs will only be updated when this value is true.\r\nDoes not affect sleep LOD.")]
        public bool updateLODs = true;
        
        /// <summary>
        /// When enabled Camera.main will be used as lod camera.
        /// </summary>
        [UnityEngine.Tooltip("When enabled Camera.main will be used as lod camera.")]
        public bool useCameraMainForLOD = true;
        
        /// <summary>
        /// Camera from which the LOD distance will be measured.
        /// To use Camera.main instead, set 'useCameraMainForLOD' to true instead.
        /// </summary>
        [UnityEngine.Tooltip("Camera from which the LOD distance will be measured.\r\nTo use Camera.main instead, set 'useCameraMainForLOD' to true instead.")]
        public Camera LODCamera;

        private Transform _cameraTransform;
        
        private float _timeSinceSpawned;
        private RigidbodyConstraints _initialRbConstraints = RigidbodyConstraints.None;
        private bool _constraintsApplied = false;
        private bool _wasFrozen;
        private int _lodCount;
        
        private float _prevAngularDrag;
        private Vector3 _prevCenterOfMass;
        private float _prevDrag;
        private Vector3 _prevInertiaTensor;
        private float _prevMass;
        private float _prevMaxAngularVelocity;

        /// <summary>
        /// True if any of the wheels are touching ground.
        /// </summary>
        public bool IsGrounded()
        {
            int wheelCount = Wheels.Count;
            for (int i = 0; i < wheelCount; i++)
            {
                if (Wheels[i].IsGrounded)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// True if all of the wheels are touching ground.
        /// </summary>
        public bool IsFullyGrounded()
        {
            int wheelCount = Wheels.Count;
            for (int i = 0; i < wheelCount; i++)
            {
                if (!Wheels[i].IsGrounded)
                {
                    return false;
                }
            }
            return true;
        }

        public List<WheelGroup> WheelGroups
        {
            get => powertrain.wheelGroups;
            set => powertrain.wheelGroups = value;
        }

        /// <summary>
        ///     List of all wheels attached to this vehicle.
        /// </summary>
        public List<WheelComponent> Wheels
        {
            get => powertrain.wheels;
            set => powertrain.wheels = value;
        }

        public Vector3 WorldEnginePosition
        {
            get => transform.TransformPoint(enginePosition);
        }

        public Vector3 WorldExhaustPosition
        {
            get => transform.TransformPoint(exhaustPosition);
        }

        public Vector3 WorldTransmissionPosition
        {
            get => transform.TransformPoint(transmissionPosition);
        }

        public override void Awake()
        {
            base.Awake();
            
            input.Awake(this);
            steering.Awake(this);
            powertrain.Awake(this);
            soundManager.Awake(this);
            effectsManager.Awake(this);
            damageHandler.Awake(this);
            brakes.Awake(this);
            groundDetection.Awake(this);
            moduleManager.Awake(this);

            _timeSinceSpawned = 0;
        }

        private void Start()
        {
            Debug.Assert(vehicleTransform != null);
            Debug.Assert(vehicleRigidbody != null);
            
            ApplyInitialRigidbodyValues();
            SetupMultiplayerInstance();

            InvokeRepeating("LODCheck", Random.Range(0.2f, 0.4f), Random.Range(0.3f, 0.5f));

            // Put to sleep immediately after initializing 
            if (!_isAwake)
            {
                Sleep();
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            
            UpdatePhysicalProperties();
            ApplyLowSpeedFixes();

            if (multiplayerInstanceType == MultiplayerInstanceType.Local)
            {
                brakes.FixedUpdate();
                steering.FixedUpdate();
                powertrain.FixedUpdate();
                moduleManager.FixedUpdate();
            }
            else
            {
                steering.FixedUpdate();
            }

            _timeSinceSpawned += fixedDeltaTime;
        }

        void ApplyInitialRigidbodyValues()
        {
            // Apply initial rigidbody values
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = GetComponent<Rigidbody>();
            }
            
            vehicleRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            vehicleRigidbody.maxAngularVelocity = maxAngularVelocity;
            vehicleRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            vehicleRigidbody.drag = drag;
            vehicleRigidbody.mass = mass;
            vehicleRigidbody.angularDrag = angularDrag;
            vehicleRigidbody.centerOfMass = centerOfMass;
            vehicleRigidbody.inertiaTensor = inertiaTensor;
            vehicleRigidbody.sleepThreshold = 0;
            _initialRbConstraints = vehicleRigidbody.constraints;
        }
        
        void UpdatePhysicalProperties()
        {
            // Set com and inertia
            if (centerOfMass != _prevCenterOfMass)
            {
                vehicleRigidbody.centerOfMass = centerOfMass;
                _prevCenterOfMass = centerOfMass;
            }

            if (inertiaTensor != _prevInertiaTensor)
            {
                vehicleRigidbody.inertiaTensor = inertiaTensor;
                _prevInertiaTensor = inertiaTensor;
            }

            if (_prevMass != mass)
            {
                vehicleRigidbody.mass = mass;
                _prevMass = mass;
            }

            if (_prevDrag != drag)
            {
                vehicleRigidbody.drag = drag;
                _prevDrag = drag;
            }

            if (_prevAngularDrag != angularDrag)
            {
                vehicleRigidbody.angularDrag = angularDrag;
                _prevAngularDrag = angularDrag;
            }

            if (_prevMaxAngularVelocity != maxAngularVelocity)
            {
                vehicleRigidbody.maxAngularVelocity = maxAngularVelocity;
                _prevMaxAngularVelocity = maxAngularVelocity;
            }
        }

        public override void Update()
        {
            base.Update();
            
            CheckComponentStates();

            if (multiplayerInstanceType == MultiplayerInstanceType.Local)
            {
                input.Update();
                effectsManager.Update();
                soundManager.Update();
                damageHandler.Update();
                moduleManager.Update();
            }
            else
            {
                effectsManager.Update();
                soundManager.Update();
            }
        }

        private void OnEnable()
        {
            vehicleTransform = transform;
            vehicleRigidbody = GetComponent<Rigidbody>();
        }

        private void OnDrawGizmosSelected()
        {
            // Draw COM
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = GetComponent<Rigidbody>();
            }

            Gizmos.color = Color.green;

#if UNITY_EDITOR
            Gizmos.color = Color.white;
            Vector3 worldComPosition = transform.TransformPoint(centerOfMass);
            Gizmos.DrawWireSphere(worldComPosition, 0.07f);
            Handles.Label(worldComPosition, new GUIContent("  CoM"));
#endif

            steering.OnDrawGizmosSelected(this);
            powertrain.OnDrawGizmosSelected(this);
            soundManager.OnDrawGizmosSelected(this);
            effectsManager.OnDrawGizmosSelected(this);
            damageHandler.OnDrawGizmosSelected(this);
            brakes.OnDrawGizmosSelected(this);
            groundDetection.OnDrawGizmosSelected(this);
            moduleManager.OnDrawGizmosSelected(this);

#if UNITY_EDITOR
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(WorldEnginePosition, 0.04f);
            Handles.Label(WorldEnginePosition, new GUIContent("  Engine"));

            Gizmos.DrawWireSphere(WorldTransmissionPosition, 0.04f);
            Handles.Label(WorldTransmissionPosition, new GUIContent("  Transmission"));

            Gizmos.DrawWireSphere(WorldExhaustPosition, 0.04f);
            Handles.Label(WorldExhaustPosition, new GUIContent("  Exhaust"));
#endif
        }

        /// <summary>
        ///     Calculates a center of mass of the vehicle based on wheel positions.
        ///     Returned value is good enough for general use but manual setting of COM is always recommended if possible.
        /// </summary>
        /// <returns>Center of mass of the vehicle's Rigidbody</returns>
        public Vector3 CaclulateCenterOfMass()
        {
            Vector3 centerOfMass = Vector3.zero;
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = GetComponent<Rigidbody>();
            }

            Vector3 centerPoint = Vector3.zero;
            Vector3 pointSum = Vector3.zero;
            int count = 0;

            foreach (WheelComponent wheel in Wheels)
            {
                pointSum += transform.InverseTransformPoint(wheel.wheelController.transform.position);
                count++;
            }

            if (count == 0)
            {
                return centerOfMass;
            }

            centerOfMass = pointSum / count;
            centerOfMass -= Wheels[0].wheelController.springLength * 0.5f * transform.up;
            return centerOfMass;
        }

        public void LODCheck()
        {
            if (stateSettings == null)
            {
                return;
            }

            _lodCount = stateSettings.LODs.Count;

            if (!_isAwake && _lodCount > 0) // Vehicle is sleeping, force the highest lod
            {
                activeLODIndex = _lodCount - 1;
                activeLOD = stateSettings.LODs[activeLODIndex];
            }
            else if (updateLODs) // Vehicle is awake, determine LOD based on distance
            {
                if (useCameraMainForLOD)
                {
                    LODCamera = Camera.main;
                }
                else
                {
                    if (LODCamera == null)
                    {
                        Debug.LogWarning("LOD camera is null. Set the LOD camera or enable 'useCameraMainForLOD' instead. Falling back to Camera.main.");
                        LODCamera = Camera.main;
                    }
                }
                
                if (_lodCount > 0 && LODCamera != null)
                {
                    _cameraTransform = LODCamera.transform;
                    stateSettings.LODs[_lodCount - 2].distance = Mathf.Infinity; // Make sure last non-sleep LOD is always matched

                    vehicleToCamDistance = Vector3.Distance(vehicleTransform.position, _cameraTransform.position);
                    for (int i = 0; i < _lodCount - 1; i++)
                    {
                        if (stateSettings.LODs[i].distance > vehicleToCamDistance)
                        {
                            activeLODIndex = i;
                            activeLOD = stateSettings.LODs[i];
                            break;
                        }
                    }
                }
                else
                {
                    activeLODIndex = -1;
                    activeLOD = null;
                }
            }
        }

        public void Reset()
        {
            SetDefaults();
        }

        public void SetColliderMaterial()
        {
            if (physicsMaterial == null)
            {
                return;
            }

            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                collider.material = physicsMaterial;
            }
        }

        /// <summary>
        ///     Resets the vehicle to default state.
        ///     Sets default values for all fields and assign default objects from resources folder.
        /// </summary>
        public void SetDefaults()
        {
            ApplyInitialRigidbodyValues();
            
            steering.SetDefaults(this);
            powertrain.SetDefaults(this);
            soundManager.SetDefaults(this);
            effectsManager.SetDefaults(this);
            damageHandler.SetDefaults(this);
            brakes.SetDefaults(this);
            groundDetection.SetDefaults(this);
            moduleManager.SetDefaults(this);

            if (stateSettings == null)
            {
                stateSettings =
                    Resources.Load(defaultResourcesPath + "DefaultStateSettings") as StateSettings;
            }

            if (physicsMaterial == null)
            {
                physicsMaterial = Resources.Load(defaultResourcesPath + "VehicleMaterial") as PhysicMaterial;
            }

            centerOfMass = CaclulateCenterOfMass();
            inertiaTensor = CalculateInertiaTensor();
        }

        public void Validate()
        {
            Debug.Log(
                $"{gameObject.name}: Validating VehicleController setup. If no other messages show up after this one, " +
                "the vehicle is good to go.");

            if (transform.localScale != Vector3.one)
            {
                Debug.LogWarning("VehicleController Transform scale is other than [1,1,1]. It is recommended to avoid " +
                                 " scaling the vehicle parent object" +
                                 " and use Scale Factor from Unity model import settings instead.");
            }
            
            steering.Validate(this);
            powertrain.Validate(this);
            soundManager.Validate(this);
            effectsManager.Validate(this);
            damageHandler.Validate(this);
            brakes.Validate(this);
            groundDetection.Validate(this);
            moduleManager.Validate(this);
        }

        public Vector3 CalculateInertiaTensor()
        {
            // Very very approximate as the positions of the individual components are not really known.
            // Still more correct than the Unity calculation which assumes uniform density of all colliders.
            Vector3 bodyInertia = new Vector3(
                (vehicleDimensions.y + vehicleDimensions.z) * 0.12f * mass,
                (vehicleDimensions.z + vehicleDimensions.x) * 0.15f * mass,
                (vehicleDimensions.x + vehicleDimensions.y) * 0.21f * mass
            );
            Vector3 wheelInertia = Vector3.zero;
            foreach (WheelComponent wheelComponent in Wheels)
            {
                Vector3 wheelLocalPos =
                    transform.InverseTransformPoint(wheelComponent.wheelController.Visual.transform.position);
                wheelInertia.x += (Mathf.Abs(wheelLocalPos.y) + Mathf.Abs(wheelLocalPos.z)) *
                                  wheelComponent.wheelController.wheel.mass;
                wheelInertia.y += (Mathf.Abs(wheelLocalPos.x) + Mathf.Abs(wheelLocalPos.z)) *
                                  wheelComponent.wheelController.wheel.mass;
                wheelInertia.z += (Mathf.Abs(wheelLocalPos.x) + Mathf.Abs(wheelLocalPos.y)) *
                                  wheelComponent.wheelController.wheel.mass;
            }

            return bodyInertia + wheelInertia;
        }

        private void OnCollisionEnter(Collision collision)
        {
            damageHandler.HandleCollision(collision);
            vehicleRigidbody.drag = drag;
            vehicleRigidbody.angularDrag = angularDrag;
        }

        private void ApplyLowSpeedFixes()
        {
            float verticalInput = input.Vertical;
            float angVelSqrMag = vehicleRigidbody.angularVelocity.sqrMagnitude;

            // Increase inertia when still to mitigate jitter at low dt.
            float t = VelocityMagnitude * 0.35f + angVelSqrMag * 1.6f;
            float inertiaScale = Mathf.Lerp(5f, 1f, t);
            vehicleRigidbody.inertiaTensor = inertiaTensor * inertiaScale;

            if ((constrainWhenAsleep || freezeWhenAsleep) && !_isAwake && t + verticalInput < 0.2f && _timeSinceSpawned > 2f)
            {
                if (freezeWhenAsleep)
                {
                    vehicleRigidbody.drag = 200f;
                }

                if (constrainWhenAsleep && !_constraintsApplied)
                {
                    _initialRbConstraints = vehicleRigidbody.constraints;
                    vehicleRigidbody.constraints = RigidbodyConstraints.FreezeAll;
                    _constraintsApplied = true;
                }
            }
            else
            {
                vehicleRigidbody.drag = Mathf.Lerp(vehicleRigidbody.drag, drag, fixedDeltaTime * 10f);
                vehicleRigidbody.constraints = _initialRbConstraints;
                _constraintsApplied = false;
            }
        }

        private void CheckComponentStates()
        {
            input.CheckState(activeLODIndex);
            steering.CheckState(activeLODIndex);
            powertrain.CheckState(activeLODIndex);
            soundManager.CheckState(activeLODIndex);
            effectsManager.CheckState(activeLODIndex);
            damageHandler.CheckState(activeLODIndex);
            brakes.CheckState(activeLODIndex);
            groundDetection.CheckState(activeLODIndex);
            moduleManager.CheckState(activeLODIndex);
        }

        public void SetupMultiplayerInstance()
        {
            if (multiplayerInstanceType == MultiplayerInstanceType.Remote)
            {
                vehicleRigidbody.isKinematic = true;
                vehicleRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                input.autoSetInput = false;

                foreach (WheelComponent wheelComponent in Wheels)
                {
                    wheelComponent.wheelController.visualOnlyUpdate = true;
                    wheelComponent.wheelController.useExternalUpdate = false;
                }

                //Sleep();
            }
            else
            {
                vehicleRigidbody.isKinematic = false;
                vehicleRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                //input.autoSetInput = true;

                foreach (WheelComponent wheelComponent in Wheels)
                {
                    wheelComponent.wheelController.visualOnlyUpdate = false;
                }
            }
        }

        public override void Wake()
        {
            _isAwake = true;

            LODCheck();

            onWake.Invoke();
        }
        
        
        public override void Sleep()
        {
            _isAwake = false;

            if (stateSettings != null)
            {
                activeLODIndex = stateSettings.LODs.Count - 1;
                activeLOD = stateSettings.LODs[activeLODIndex];
            }

            onSleep.Invoke();
        }
    }
}