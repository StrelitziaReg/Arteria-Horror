using System;
using NWH.Common.Input;
using UnityEngine;

namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Class for handling desktop user input via mouse and keyboard through InputManager.
    /// </summary>
    [RequireComponent(typeof(InputManagerSceneInputProvider))]
    public class InputManagerVehicleInputProvider : VehicleInputProviderBase
    {
        /// <summary>
        ///     Names of input bindings for each individual gears. If you need to add more gears modify this and the corresponding
        ///     iterator in the
        ///     ShiftInto() function.
        /// </summary>
        [NonSerialized]
        [Tooltip(
            "Names of input bindings for each individual gears. If you need to add more gears modify this and the corresponding\r\niterator in the\r\nShiftInto() function.")]
        public string[] shiftInputNames =
        {
            "ShiftIntoR1",
            "ShiftInto0",
            "ShiftInto1",
            "ShiftInto2",
            "ShiftInto3",
            "ShiftInto4",
            "ShiftInto5",
            "ShiftInto6",
            "ShiftInto7",
            "ShiftInto8",
            "ShiftInto9"
        };

        private string _tmpStr;

        public static int warningCount = 0;
        
        // *** VEHICLE BINDINGS ***
        public override float Steering()
        {
            return InputUtils.TryGetAxisRaw("Steering");
        }
        
        public override float Throttle()
        {
            return Mathf.Clamp01(InputUtils.TryGetAxisRaw("Throttle"));
        }
        
        public override float Brakes()
        {
            return Mathf.Clamp01(InputUtils.TryGetAxisRaw("Brakes"));
        }
        
        public override float Clutch()
        {
            return Mathf.Clamp01(InputUtils.TryGetAxis("Clutch"));
        }
        
        public override float Handbrake()
        {
            return Mathf.Clamp01(InputUtils.TryGetAxis("Handbrake"));
        }


        public override bool EngineStartStop()
        {
            return InputUtils.TryGetButtonDown("EngineStartStop", KeyCode.E);
        }

        public override bool ExtraLights()
        {
            return InputUtils.TryGetButtonDown("ExtraLights", KeyCode.Semicolon);
        }


        public override bool HighBeamLights()
        {
            return InputUtils.TryGetButtonDown("HighBeamLights", KeyCode.K);
        }
        

        public override bool HazardLights()
        {
            return InputUtils.TryGetButtonDown("HazardLights", KeyCode.J);
        }

        public override bool Horn()
        {
            return InputUtils.TryGetButton("Horn", KeyCode.H);
        }

        public override bool LeftBlinker()
        {
            return InputUtils.TryGetButtonDown("LeftBlinker", KeyCode.Z);
        }

        public override bool LowBeamLights()
        {
            return InputUtils.TryGetButtonDown("LowBeamLights", KeyCode.L);
        }

        public override bool RightBlinker()
        {
            return InputUtils.TryGetButtonDown("RightBlinker", KeyCode.X);
        }

        public override bool ShiftDown()
        {
            return InputUtils.TryGetButtonDown("ShiftDown", KeyCode.F);
        }

        /// <summary>
        ///     Used for H-shifters and direct shifting into gear on non-sequential gearboxes.
        /// </summary>
        public override int ShiftInto()
        {
            for (int i = -1; i < 9; i++)
            {
                if (InputUtils.TryGetButton(shiftInputNames[i + 1], KeyCode.Alpha0, false))
                {
                    return i;
                }
            }

            return -999;
        }

        public override bool ShiftUp()
        {
            return InputUtils.TryGetButtonDown("ShiftUp", KeyCode.R);
        }

        public override bool TrailerAttachDetach()
        {
            return InputUtils.TryGetButtonDown("TrailerAttachDetach", KeyCode.T);
        }

        public override bool FlipOver()
        {
            return InputUtils.TryGetButtonDown("FlipOver", KeyCode.M);
        }

        public override bool Boost()
        {
            return InputUtils.TryGetButton("Boost", KeyCode.LeftShift);
        }

        public override bool CruiseControl()
        {
            return InputUtils.TryGetButtonDown("CruiseControl", KeyCode.N);
        }
    }
}