using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace NWH.VehiclePhysics2.Effects
{
    /// <summary>
    ///     Class for controlling all of the vehicle lights.
    /// </summary>
    [Serializable]
    public class LightsMananger : Effect
    {
        /// <summary>
        ///     Rear lights that will light up when brake is pressed. Always red.
        /// </summary>
        [FormerlySerializedAs("stopLights")]
        [Tooltip("    Rear lights that will light up when brake is pressed. Always red.")]
        public VehicleLight brakeLights = new VehicleLight();

        /// <summary>
        ///     Can be used for any type of special lights, e.g. beacons.
        /// </summary>
        [UnityEngine.Tooltip("    Can be used for any type of special lights, e.g. beacons.")]
        public VehicleLight extraLights = new VehicleLight();

        /// <summary>
        ///     High (full) beam lights.
        /// </summary>
        [FormerlySerializedAs("fullBeams")]
        [Tooltip("    High (full) beam lights.")]
        public VehicleLight highBeamLights = new VehicleLight();

        /// <summary>
        ///     Blinkers on the left side of the vehicle.
        /// </summary>
        [Tooltip("    Blinkers on the left side of the vehicle.")]
        public VehicleLight leftBlinkers = new VehicleLight();

        /// <summary>
        ///     Low beam lights.
        /// </summary>
        [FormerlySerializedAs("headLights")]
        [Tooltip("    Low beam lights.")]
        public VehicleLight lowBeamLights = new VehicleLight();

        /// <summary>
        ///     Rear Lights that will light up when vehicle is in reverse gear(s). Usually white.
        /// </summary>
        [Tooltip("    Rear Lights that will light up when vehicle is in reverse gear(s). Usually white.")]
        public VehicleLight reverseLights = new VehicleLight();

        /// <summary>
        ///     Blinkers on the right side of the vehicle.
        /// </summary>
        [Tooltip("    Blinkers on the right side of the vehicle.")]
        public VehicleLight rightBlinkers = new VehicleLight();

        /// <summary>
        ///     Rear Lights that will light up when headlights are on. Always red.
        /// </summary>
        [FormerlySerializedAs("rearLights")]
        [Tooltip("    Rear Lights that will light up when headlights are on. Always red.")]
        public VehicleLight tailLights = new VehicleLight();

        private bool _hazardLightsOn = false;
        private bool _leftBlinkersOn = false;
        private bool _rightBlinkersOn = false;
        private float _leftBlinkerTurnOnTime;
        private float _rightBlinkerTurnOnTime;
        
        public bool LeftBlinkerState
        {
            get => (int) ((Time.realtimeSinceStartup - _leftBlinkerTurnOnTime) * 2) % 2 == 0;
        }

        public bool RightBlinkerState
        {
            get => (int) ((Time.realtimeSinceStartup - _rightBlinkerTurnOnTime) * 2) % 2 == 0;
        }
        
        public override void FixedUpdate()
        {
        }

        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            if (IsEnabled)
            {
                // Stop lights
                if (vc.brakes.IsBraking)
                {
                    brakeLights?.TurnOn();
                }
                else
                {
                    brakeLights?.TurnOff();
                }
                
                // Reversing lights
                if (vc.powertrain.transmission.Gear < 0)
                {
                    reverseLights.TurnOn();
                }
                else
                {
                    reverseLights.TurnOff();
                }

                // Low beam lights
                if (lowBeamLights != null)
                {
                    if (vc.input.states.lowBeamLights)
                    {
                        lowBeamLights.Toggle();
                        if (lowBeamLights.On)
                        {
                            tailLights.TurnOn();
                        }
                        else
                        {
                            tailLights.TurnOff();
                        }
                        vc.input.states.lowBeamLights = false;
                    }
                }
                
                if (highBeamLights != null && lowBeamLights != null)
                {
                    if (vc.input.states.highBeamLights)
                    {
                        bool prevState = highBeamLights.On;
                        highBeamLights.Toggle();
                        if (highBeamLights.On && !prevState)
                        {
                            lowBeamLights.TurnOn();
                        }
                        vc.input.states.highBeamLights = false;
                    }
                }

                // Blinkers and hazards
                if (leftBlinkers != null && rightBlinkers != null)
                {
                    if (vc.input.states.hazardLights)
                    {
                        _hazardLightsOn = !_hazardLightsOn;
                        _leftBlinkersOn = _rightBlinkersOn = _hazardLightsOn;
                        if (_hazardLightsOn)
                        {
                            _leftBlinkerTurnOnTime = _rightBlinkerTurnOnTime = Time.realtimeSinceStartup;
                        }
                        else
                        {
                            _leftBlinkersOn = false;
                            _rightBlinkersOn = false;
                        }
                        vc.input.states.hazardLights = false;
                    }

                    if (!_hazardLightsOn)
                    {
                        if (vc.input.states.leftBlinker)
                        {
                            _leftBlinkersOn = !_leftBlinkersOn;
                            if (_leftBlinkersOn)
                            {
                                _leftBlinkerTurnOnTime = Time.realtimeSinceStartup;
                                _rightBlinkersOn = false;
                            }

                            vc.input.states.leftBlinker = false;
                        }

                        if (vc.input.states.rightBlinker)
                        {
                            _rightBlinkersOn = !_rightBlinkersOn;
                            if (_rightBlinkersOn)
                            {
                                _rightBlinkerTurnOnTime = Time.realtimeSinceStartup;
                                _leftBlinkersOn = false;
                            }

                            vc.input.states.rightBlinker = false;
                        }
                    }
                    else
                    {
                        vc.input.states.leftBlinker = false;
                        vc.input.states.rightBlinker = false;
                    }

                    leftBlinkers.SetState(_leftBlinkersOn && LeftBlinkerState);
                    rightBlinkers.SetState(_rightBlinkersOn && RightBlinkerState);
                }
                
                // Extra lights
                if (extraLights != null)
                {
                    if (vc.input.states.extraLights)
                    {
                        extraLights.Toggle();
                        vc.input.states.extraLights = false;
                    }
                }
            }
        }

        public override void Disable()
        {
            base.Disable();

            TurnOffAllLights();
        }

        /// <summary>
        ///     Returns light states as a byte with each bit representing one light;
        /// </summary>
        public byte GetByteState()
        {
            byte state = 0;

            if (brakeLights.On)
            {
                state |= 1 << 0;
            }

            if (tailLights.On)
            {
                state |= 1 << 1;
            }

            if (reverseLights.On)
            {
                state |= 1 << 2;
            }

            if (lowBeamLights.On)
            {
                state |= 1 << 3;
            }

            if (highBeamLights.On)
            {
                state |= 1 << 4;
            }

            if (leftBlinkers.On)
            {
                state |= 1 << 5;
            }

            if (rightBlinkers.On)
            {
                state |= 1 << 6;
            }

            if (extraLights.On)
            {
                state |= 1 << 7;
            }

            return state;
        }


        /// <summary>
        ///     Sets state of lights from a single byte where each bit represents one light.
        ///     To be used with GetByteState().
        /// </summary>
        /// <param name="state"></param>
        public void SetByteState(byte state)
        {
            if ((state & (1 << 0)) != 0)
            {
                brakeLights.TurnOn();
            }
            else
            {
                brakeLights.TurnOff();
            }

            if ((state & (1 << 1)) != 0)
            {
                tailLights.TurnOn();
            }
            else
            {
                tailLights.TurnOff();
            }

            if ((state & (1 << 2)) != 0)
            {
                reverseLights.TurnOn();
            }
            else
            {
                reverseLights.TurnOff();
            }

            if ((state & (1 << 3)) != 0)
            {
                lowBeamLights.TurnOn();
            }
            else
            {
                lowBeamLights.TurnOff();
            }

            if ((state & (1 << 4)) != 0)
            {
                highBeamLights.TurnOn();
            }
            else
            {
                highBeamLights.TurnOff();
            }

            if ((state & (1 << 5)) != 0)
            {
                leftBlinkers.TurnOn();
            }
            else
            {
                leftBlinkers.TurnOff();
            }

            if ((state & (1 << 6)) != 0)
            {
                rightBlinkers.TurnOn();
            }
            else
            {
                rightBlinkers.TurnOff();
            }
        }

        /// <summary>
        ///     Sets state of lights from a single byte where each bit represents one light.
        ///     To be used with GetByteState().
        /// </summary>
        /// <param name="state"></param>
        public void SetStatesFromByte(byte state)
        {
            if ((state & (1 << 0)) != 0)
            {
                brakeLights.TurnOn();
            }
            else
            {
                brakeLights.TurnOff();
            }

            if ((state & (1 << 1)) != 0)
            {
                tailLights.TurnOn();
            }
            else
            {
                tailLights.TurnOff();
            }

            if ((state & (1 << 2)) != 0)
            {
                reverseLights.TurnOn();
            }
            else
            {
                reverseLights.TurnOff();
            }

            if ((state & (1 << 3)) != 0)
            {
                lowBeamLights.TurnOn();
            }
            else
            {
                lowBeamLights.TurnOff();
            }

            if ((state & (1 << 4)) != 0)
            {
                highBeamLights.TurnOn();
            }
            else
            {
                highBeamLights.TurnOff();
            }

            if ((state & (1 << 5)) != 0)
            {
                leftBlinkers.TurnOn();
            }
            else
            {
                leftBlinkers.TurnOff();
            }

            if ((state & (1 << 6)) != 0)
            {
                rightBlinkers.TurnOn();
            }
            else
            {
                rightBlinkers.TurnOff();
            }

            if ((state & (1 << 7)) != 0)
            {
                extraLights.TurnOn();
            }
            else
            {
                extraLights.TurnOff();
            }
        }

        /// <summary>
        ///     Turns off all lights and emission on all meshes.
        /// </summary>
        public void TurnOffAllLights()
        {
            brakeLights.TurnOff();
            lowBeamLights.TurnOff();
            tailLights.TurnOff();
            reverseLights.TurnOff();
            highBeamLights.TurnOff();
            leftBlinkers.TurnOff();
            rightBlinkers.TurnOff();
            extraLights.TurnOff();
        }
    }
}