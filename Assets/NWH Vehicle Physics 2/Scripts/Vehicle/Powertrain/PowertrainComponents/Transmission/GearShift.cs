

using UnityEngine;

namespace NWH.VehiclePhysics2.Powertrain
{
    /// <summary>
    /// Class that stores gear shift data.
    /// </summary>
    public class GearShift
    {
        public void RegisterShift(float currentTime, float duration, int toGear, int fromGear)
        {
            this._startTime = currentTime;
            this._duration = duration;
            this._toGear = toGear;
            this._fromGear = fromGear;
            //Debug.Log($"Shifting from {_fromGear} to {_toGear} in {_duration}s.");
        }
        
        private float _duration;
        private float _startTime;
        private int _fromGear;
        private int _toGear;

        /// <summary>
        /// Duration of the current gear shift.
        /// </summary>
        public float Duration
        {
            get => _duration;
            set => _duration = value;
        }

        /// <summary>
        /// Gear from which the gear shift starts.
        /// </summary>
        public int FromGear
        {
            get => _fromGear;
            set => _fromGear = value;
        }

        /// <summary>
        /// Target gear.
        /// </summary>
        public int ToGear
        {
            get => _toGear;
            set => _toGear = value;
        }

        /// <summary>
        /// Start time of a gear shift.
        /// </summary>
        public float StartTime
        {
            get => _startTime;
            set => _startTime = value;
        }

        /// <summary>
        /// End time of a gear shift. Calculated from start time and duration.
        /// </summary>
        public float EndTime
        {
            get => _startTime + _duration;
        }

        /// <summary>
        /// Has the gear shift ended? If true the GearShift is not valid anymore.
        /// </summary>
        public bool HasEnded
        {
            get => Time.realtimeSinceStartup > EndTime;
        }

        /// <summary>
        /// True when shifting to a larger gear. Works in reverse too.
        /// Example: 2 to 3, -2 to -3.
        /// </summary>
        public bool IsUpshift
        {
            get => Mathf.Abs(_fromGear) < Mathf.Abs(_toGear);
        }

        /// <summary>
        /// Opposite of IsUpshift.
        /// </summary>
        public bool IsDownshift
        {
            get => !IsUpshift;
        }
    }
}