using System;
using System.Collections.Generic;
using NWH.Common.Input;
using UnityEngine;
using UnityEngine.Serialization;

namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Manages vehicle input by retrieving it from the active InputProvider and filling in the InputStates with the
    ///     fetched data.
    /// </summary>
    [Serializable]
    public class Input : VehicleComponent
    {
        public const float DEADZONE = 0.02f;

        /// <summary>
        /// When enabled input will be auto-retrieved from the InputProviders present in the scene.
        /// Disable to manualy set the input through external scripts, i.e. AI controller.
        /// </summary>
        [FormerlySerializedAs("autoSettable")] public bool autoSetInput = true;
        
        /// <summary>
        /// All the input states of the vehicle. Can be used to set input through scripting or copy the inputs
        /// over from other vehicle, such as truck to trailer.
        /// </summary>
        [UnityEngine.Tooltip("All the input states of the vehicle. Can be used to set input through scripting or copy the inputs\r\nover from other vehicle, such as truck to trailer.")]
        public InputStates states;
        
        /// <summary>
        /// Swaps throttle and brake axes when vehicle is in reverse.
        /// </summary>
        [UnityEngine.Tooltip("Swaps throttle and brake axes when vehicle is in reverse.")]
        public bool swapInputInReverse = true;
        private List<InputProvider>  _inputProviders = new List<InputProvider>();

        private delegate bool BinaryInputDelegate();
        
        /// <summary>
        /// Convenience function for setting throttle/brakes as a single value.
        /// Use Throttle/Brake axes to apply throttle and braking separately.
        /// If the set value is larger than 0 throttle will be set, else if value is less than 0 brake axis will be set.
        /// </summary>
        public float Vertical
        {
            get => states.throttle + states.brakes;
            set
            {
                float clampedValue = value < -1 ? -1 : value > 1 ? 1 : value;
                if (value > 0)
                {
                    states.throttle = clampedValue;
                    states.brakes = 0;
                }
                else
                {
                    states.throttle = 0;
                    states.brakes = -clampedValue;
                }
            }
        }

        /// <summary>
        /// Throttle axis.
        /// For combined throttle/brake input (such as prior to v1.0.1) use 'Vertical' instead.
        /// </summary>
        public float Throttle
        {
            get => states.throttle;
            set => states.throttle = value < 0f ? 0f : value > 1f ? 1f : value;
        }

        /// <summary>
        /// Brake axis.
        /// For combined throttle/brake input use 'Vertical' instead.
        /// </summary>
        public float Brakes
        {
            get => states.brakes;
            set => states.brakes = value < 0f ? 0f : value > 1f ? 1f : value;
        }

        /// <summary>
        /// Returns throttle or brake input based on 'swapInputInReverse' setting and current gear.
        /// If swapInputInReverse is true, brake will act as throttle and vice versa while driving in reverse.
        /// </summary>
        public float InputSwappedThrottle
        {
            get => IsInputSwapped ? Brakes : Throttle;
        }

        /// <summary>
        /// Returns throttle or brake input based on 'swapInputInReverse' setting and current gear.
        /// If swapInputInReverse is true, throttle will act as brake and vise versa while driving in reverse.
        /// </summary>
        public float InputSwappedBrakes
        {
            get => IsInputSwapped ? Throttle : Brakes;
        }

        /// <summary>
        /// Steering axis.
        /// </summary>
        public float Steering
        {
            get => states.steering;
            set => states.steering = value < -1f ? -1f : value > 1f ? 1f : value;
        }

        /// <summary>
        /// Clutch axis.
        /// </summary>
        public float Clutch
        {
            get => states.clutch;
            set => states.clutch = value < 0f ? 0f : value > 1f ? 1f : value;
        }

        public bool EngineStartStop
        {
            get => states.engineStartStop;
            set => states.engineStartStop = value;
        }

        public bool ExtraLights
        {
            get => states.extraLights;
            set => states.extraLights = value;
        }

        public bool HighBeamLights
        {
            get => states.highBeamLights;
            set => states.highBeamLights = value;
        }

        public float Handbrake
        {
            get => states.handbrake;
            set => states.handbrake = value < 0f ? 0f : value > 1f ? 1f : value;
        }

        public bool HazardLights
        {
            get => states.hazardLights;
            set => states.hazardLights = value;
        }

        public bool Horn
        {
            get => states.horn;
            set => states.horn = value;
        }

        public bool LeftBlinker
        {
            get => states.leftBlinker;
            set => states.leftBlinker = value;
        }

        public bool LowBeamLights
        {
            get => states.lowBeamLights;
            set => states.lowBeamLights = value;
        }

        public bool RightBlinker
        {
            get => states.rightBlinker;
            set => states.rightBlinker = value;
        }

        public bool ShiftDown
        {
            get => states.shiftDown;
            set => states.shiftDown = value;
        }

        public int ShiftInto
        {
            get => states.shiftInto;
            set => states.shiftInto = value;
        }

        public bool ShiftUp
        {
            get => states.shiftUp;
            set => states.shiftUp = value;
        }

        public bool TrailerAttachDetach
        {
            get => states.trailerAttachDetach;
            set => states.trailerAttachDetach = value;
        }

        public bool CruiseControl
        {
            get => states.cruiseControl;
            set => states.cruiseControl = value;
        }

        public bool Boost
        {
            get => states.boost;
            set => states.boost = value;
        }

        public bool FlipOver
        {
            get => states.flipOver;
            set => states.flipOver = value;
        }

        /// <summary>
        /// True when throttle and brake axis are swapped.
        /// </summary>
        public bool IsInputSwapped
        {
            get => swapInputInReverse && vc.powertrain.transmission.IsInReverse;
        }

        public override void Initialize()
        {
            _inputProviders = InputProvider.Instances;

            if (_inputProviders == null || _inputProviders.Count == 0)
            {
                Debug.LogWarning(
                    "No InputProviders are present in the scene. Make sure that one or more InputProviders are present (DesktopInputProvider, MobileInputProvider, etc.).");
                return;
            }

            states.Reset(); // Reset states to make sure that initial values are neutral in case the behaviour was copied or similar.
            base.Initialize();
        }

        public override void FixedUpdate()
        {
        }

        public override void Update()
        {
            if (!Active || !autoSetInput)
            {
                return;
            }

            Throttle = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Throttle());
            Brakes = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Brakes());

            Steering = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Steering());
            Clutch = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Clutch());
            Handbrake = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Handbrake());
            ShiftInto = CombinedInputGear<VehicleInputProviderBase>(i => i.ShiftInto());

            ShiftUp |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.ShiftUp());
            ShiftDown |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.ShiftDown());
            
            LeftBlinker |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.LeftBlinker());
            RightBlinker |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.RightBlinker());
            LowBeamLights |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.LowBeamLights());
            HighBeamLights |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.HighBeamLights());
            HazardLights |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.HazardLights());
            ExtraLights |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.ExtraLights());
            
            Horn = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Horn());
            EngineStartStop |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.EngineStartStop());
            
            Boost = InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.Boost());
            TrailerAttachDetach = TrailerAttachDetach || InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.TrailerAttachDetach());
            CruiseControl |= InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.CruiseControl());
            FlipOver = FlipOver || InputProvider.CombinedInput<VehicleInputProviderBase>(i => i.FlipOver());
        }
        
                
        public static int CombinedInputGear<T>(Func<T, int> selector) where T : InputProvider
        {
            int gear = -999;
            foreach (InputProvider ip in InputProvider.Instances)
            {
                if (ip is T)
                {
                    int tmp = selector(ip as T);
                    if (tmp > gear)
                    {
                        gear = tmp;
                    }
                }
            }
            return gear;
        }

        public override void Disable()
        {
            base.Disable();
            states.Reset();
        }

        public void ResetShiftFlags()
        {
            states.shiftUp = false;
            states.shiftDown = false;
            states.shiftInto = -999;
        }
    }
}