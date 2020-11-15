using NWH.NUI;
using UnityEditor;
using UnityEngine;

namespace NWH.VehiclePhysics2.SetupWizard
{
    [CustomEditor(typeof(VehicleSetupWizard))]
    [CanEditMultipleObjects]
    public class VehicleSetupWizardEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            VehicleSetupWizard sw = drawer.GetObject<VehicleSetupWizard>();
            if (sw == null)
            {
                return false;
            }

            if (PrefabUtility.GetPrefabInstanceStatus(sw.gameObject) == PrefabInstanceStatus.Connected)
            {
                drawer.Info("Setup Wizard should not be run in prefab mode, only scene mode.", MessageType.Warning);
            }

            drawer.BeginSubsection("Collider");
            if (drawer.Field("addCollider").boolValue)
            {
                drawer.Field("bodyMeshGameObject");
            }
            else
            {
                drawer.Info("When setting up collider(s) manually make sure that all colliders are either on" +
                            " 'Physics.IgnoreRaycast' or one of the WheelController's ignore layers.");
            }

            drawer.EndSubsection();

            drawer.BeginSubsection("Wheels");
            drawer.Info(
                "Wheel GameObjects should be added in the left-right, front-to-back order e.g. frontLeft, frontRight, rearLeft, rearRight");
            drawer.ReorderableList("wheelGameObjects");
            drawer.EndSubsection();

            drawer.BeginSubsection("Options");
            drawer.Field("addCamera");
            drawer.Field("addCharacterEnterExitPoints");

            drawer.EndSubsection();

            drawer.HorizontalRuler();

            VehicleController generatedVehicleController = null;
            if (drawer.Button("Run Setup"))
            {
                generatedVehicleController = VehicleSetupWizard.RunSetup(sw.gameObject, sw.wheelGameObjects, 
                    sw.bodyMeshGameObject, sw.addCollider, sw.addCamera, sw.addCharacterEnterExitPoints);
            }
            
            drawer.Info("InputProvider will not be added automatically to the scene. Add VehicleInputProvider and SceneInputProvider" +
                        "for used input type if they are already not present (e.g. InputSystemVehicleInputProvider and InputSystemSceneInputProvider. " +
                        "Without this vehicle will not receive input.", MessageType.Warning);

            drawer.EndEditor(this);
            if(generatedVehicleController != null)
            {
                DestroyImmediate(sw);
            }

            return true;
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }
    }
}