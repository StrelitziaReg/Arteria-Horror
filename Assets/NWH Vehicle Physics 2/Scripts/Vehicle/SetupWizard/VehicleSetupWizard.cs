using System.Collections.Generic;
using NWH.Common.Cameras;
using NWH.Common.Input;
using NWH.VehiclePhysics2.Input;
using NWH.WheelController3D;
using UnityEngine;
using UnityEngine.Serialization;

namespace NWH.VehiclePhysics2.SetupWizard
{
    /// <summary>
    /// Script used to set up vehicle from a model.
    /// Can be used through editor or called at run-time.
    /// Requires model with separate wheels and Unity-correct scale, rotation and pivots.
    /// </summary>
    public class VehicleSetupWizard : MonoBehaviour
    {
        /// <summary>
        /// Should a default vehicle camera and camera changer be added?
        /// </summary>
        public bool addCamera = true;
        
        /// <summary>
        /// Should character enter/exit points be added?
        /// </summary>
        public bool addCharacterEnterExitPoints = true;
        
        /// <summary>
        /// Should MeshCollider be added to bodyMeshGO?
        /// </summary>
        public bool addCollider;
        
        /// <summary>
        /// GameObject to which the body MeshCollider will be added. Leave null if it has already been set up.
        /// It is not recommended to run the setup without any colliders being previously present as this will void inertia and center of mass
        /// calculations during the setup.
        /// </summary>
        public GameObject bodyMeshGameObject;
        
        /// <summary>
        /// Wheel GameObjects in order: front-left, front-right, rear-left, rear-right, etc.
        /// </summary>
        public List<GameObject> wheelGameObjects = new List<GameObject>();
        
        private GameObject _cameraParent;
        private GameObject _wheelControllerParent;

        // Group parents
        private GameObject _wheelParent;

        /// <summary>
        /// Sets up a vehicle from scratch. Requires only a model with proper scale, rotation and pivots.
        /// </summary>
        /// <param name="targetGO">Root GameObject of the vehicle.</param>
        /// <param name="wheelGOs">Wheel GameObjects in order: front-left, front-right, rear-left, rear-right, etc.</param>
        /// <param name="bodyMeshGO">GameObject to which the body MeshCollider will be added. Leave null if it has already been set up.
        /// It is not recommended to run the setup without any colliders being previously present as this will void inertia and center of mass
        /// calculations during the setup.</param>
        /// <param name="addCollider">Should MeshCollider be added to bodyMeshGO?</param>
        /// <param name="addCamera">Should a default vehicle camera and camera changer be added?</param>
        /// <param name="addCharacterEnterExitPoints">Should character enter/exit points be added?</param>
        /// <param name="verboseLevel">0 - show errors only, 1 - show errors and warnings, 2 - show errors, warnings and info messages.</param>
        /// <returns>Returns newly created VehicleController if setup is successful or null if not.</returns>
        public static VehicleController RunSetup(GameObject targetGO, List<GameObject> wheelGOs, GameObject bodyMeshGO = null, 
            bool addCollider = true, bool addCamera = true, bool addCharacterEnterExitPoints = true, int verboseLevel = 2)
        {
            if(verboseLevel == 2) Debug.Log("======== VEHICLE SETUP START ========");

            Transform transform = targetGO.transform;
            
            if (transform.localScale != Vector3.one)
            {
                if(verboseLevel >= 1) Debug.LogWarning(
                    "Scale of a parent object should be [1,1,1] for Rigidbody and VehicleController to function properly.");
                return null;
            }

            // Set vehicle tag
            targetGO.tag = "Vehicle";

            // Add body collider
            if (bodyMeshGO != null)
            {
                MeshCollider bodyCollider = bodyMeshGO.GetComponent<MeshCollider>();
                if (bodyCollider == null)
                {
                    if(verboseLevel == 2) Debug.Log($"Adding MeshCollider to body mesh object {bodyMeshGO.name}");

                    // Add mesh collider to body mesh
                    bodyCollider = bodyMeshGO.AddComponent<MeshCollider>();
                    bodyCollider.convex = true;

                    // Set body mesh layer to 'Ignore Raycast' to prevent wheels colliding with the vehicle itself.
                    // This is the default value, you can use other layers by changing the Ignore Layer settings under WheelController inspector.
                    if(verboseLevel == 2) Debug.Log(
                        "Setting layer of body collider to default layer 'Ignore Raycast' to prevent wheels from detecting the vehicle itself." +
                        " If you wish to use some other layer check Ignore Layer settings (WheelController inspector).");
                    bodyMeshGO.layer = 2;
                }
            }

            // Add rigidbody
            Rigidbody vehicleRigidbody = targetGO.GetComponent<Rigidbody>();
            if (vehicleRigidbody == null)
            {
                if(verboseLevel == 2) Debug.Log($"Adding Rigidbody to {targetGO.name}");

                // Add a rigidbody. No need to change rigidbody values as those are set by the VehicleController
                vehicleRigidbody = targetGO.gameObject.AddComponent<Rigidbody>();
            }

            // Create WheelController GOs and add WheelControllers
            foreach (GameObject wheelObject in wheelGOs)
            {
                string objName = $"{wheelObject.name}_WheelController";
                if(verboseLevel == 2) Debug.Log($"Creating new WheelController object {objName}");

                if (!transform.Find(objName))
                {
                    GameObject wcGo = new GameObject(objName);
                    wcGo.transform.SetParent(transform);

                    // Position the WheelController GO to the same position as the wheel
                    wcGo.transform.SetPositionAndRotation(wheelObject.transform.position,
                        wheelObject.transform.rotation);

                    // Move spring anchor to be above the wheel
                    wcGo.transform.position += transform.up * 0.2f;

                    if(verboseLevel == 2) Debug.Log($"   |-> Adding WheelController to {wcGo.name}");

                    // Add WheelController
                    WheelController wheelController = wcGo.AddComponent<WheelController>();

                    // Assign visual to WheelController
                    wheelController.Visual = wheelObject;

                    // Attempt to find radius and width
                    MeshRenderer mr = wheelObject.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        float radius = mr.bounds.extents.y;
                        if (radius < 0.05f || radius > 1f)
                        {
                            if(verboseLevel >= 1) Debug.LogWarning(
                                "Detected unusual wheel radius. Adjust WheelController's radius field manually.");
                        }

                        if(verboseLevel == 2) Debug.Log($"   |-> Setting radius to {radius}");
                        wheelController.wheel.radius = radius;

                        float width = mr.bounds.extents.x * 2f;
                        if (width < 0.02f || width > 1f)
                        {
                            if(verboseLevel >= 1) Debug.LogWarning(
                                "Detected unusual wheel width. Adjust WheelController's width field manually.");
                        }

                        if(verboseLevel == 2) Debug.Log($"   |-> Setting width to {width}");
                        wheelController.wheel.width = width;
                    }
                    else
                    {
                        if(verboseLevel >= 1) Debug.LogWarning(
                            $"Radius and width could not be auto configured. Wheel {wheelObject.name} does not contain a MeshFilter.");
                    }
                }
            }

            // Add VehicleController
            VehicleController vehicleController = targetGO.GetComponent<VehicleController>();
            if (vehicleController == null)
            {
                if(verboseLevel == 2) Debug.Log($"Adding VehicleController to {targetGO.name}");
                vehicleController = targetGO.AddComponent<VehicleController>();
                vehicleController.SetDefaults();
            }

            // Add camera
            if (addCamera)
            {
                if(verboseLevel == 2) Debug.Log("Adding CameraChanger.");
                GameObject camerasContainer = new GameObject("Cameras");
                camerasContainer.transform.SetParent(transform);
                CameraChanger cameraChanger = camerasContainer.AddComponent<CameraChanger>();

                if(verboseLevel == 2) Debug.Log("Adding a camera follow.");
                GameObject cameraGO = new GameObject("Vehicle Camera");
                cameraGO.transform.SetParent(camerasContainer.transform);
                var t = vehicleController.transform;
                cameraGO.transform.SetPositionAndRotation(t.position, t.rotation);

                Camera camera = cameraGO.AddComponent<Camera>();
                camera.fieldOfView = 80f;

                cameraGO.AddComponent<AudioListener>();

                CameraFollow cameraFollow = cameraGO.AddComponent<CameraFollow>();
                cameraFollow.target = vehicleController as Vehicle;
                cameraFollow.tag = "MainCamera";
            }

            if (addCharacterEnterExitPoints)
            {
                if(verboseLevel == 2) Debug.Log("Adding enter/exit points.");
                GameObject leftPoint = new GameObject("LeftEnterExitPoint");
                GameObject rightPoint = new GameObject("RightEnterExitPoint");

                leftPoint.transform.SetParent(transform);
                rightPoint.transform.SetParent(transform);

                leftPoint.transform.position = transform.position + transform.right;
                rightPoint.transform.position = transform.position - transform.right;

                leftPoint.tag = "EnterExitPoint";
                rightPoint.tag = "EnterExitPoint";
            }

            // Validate setup
            if(verboseLevel == 2) Debug.Log("Validating setup.");

            // Run Validate() on VehicleController which will report if there are any problems with the setup.
            vehicleController.Validate();

            if(verboseLevel == 2) Debug.Log("Setup done.");

            if(verboseLevel == 2) Debug.Log("======== VEHICLE SETUP END ========");

            return vehicleController;
        }
    }
}